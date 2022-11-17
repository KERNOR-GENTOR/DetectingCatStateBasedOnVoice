#include "hw_config.h"
#include "usb_lib.h"
#include "usb_pwr.h"
#include "math.h"
#include <stdio.h> 
// ---> usb_endp.c
extern uint8_t packet_send;
// <--- usb_endp.c
/* Private variables ---------------------------------------------------------*/
#define ADC_BUF_SIZE    1024 // ADC buffer size
#define ADC_BUF_MASK ADC_BUF_SIZE-1
#define OVS_COUNT       8   // Oversampling count
#define TIM_PERIOD (72000000/(32000*OVS_COUNT))-1//72000000/(256000)-1 = 280
#define ADC_INJECTED_OFFSET 1900
#define USB_SEND_PACKET_SIZE 64
#define SILENCE_LEVEL   200
uint8_t adc_buffer[ADC_BUF_SIZE];
uint32_t usb_buf_pos = 0;
uint32_t adc_buf_pos = 0;
volatile int32_t ovs_acc = 0;
volatile uint16_t ovs_cnt = 0;
volatile int16_t adc_val;
//+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++//
//============================== SetSysClock ====================================//
//+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++//
void SetSysClock(void)
{
	  ErrorStatus HSEStartUpStatus;
    RCC_DeInit();
    RCC_HSEConfig( RCC_HSE_ON);
    HSEStartUpStatus = RCC_WaitForHSEStartUp();

    if (HSEStartUpStatus == SUCCESS)
    {
    	 FLASH_PrefetchBufferCmd( FLASH_PrefetchBuffer_Enable);

        FLASH_SetLatency( FLASH_Latency_2);
        RCC_HCLKConfig( RCC_SYSCLK_Div1);
        RCC_PCLK2Config( RCC_HCLK_Div1);
        RCC_PCLK1Config( RCC_HCLK_Div2);      
        RCC_PLLConfig(0x00010000, RCC_PLLMul_9);// PLLCLK = 8MHz * 9 = 72 MHz 
        RCC_PLLCmd( ENABLE);
        while (RCC_GetFlagStatus(RCC_FLAG_PLLRDY) == RESET)
        {
        }
        RCC_SYSCLKConfig( RCC_SYSCLKSource_PLLCLK);
        while (RCC_GetSYSCLKSource() != 0x08)
        {
        }
    }
    else
    { 
        while (1)
        {
        }
    }
}
//+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++//
//================================ tim4_ini =====================================//
//+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++//
void tim4_ini(void){//in 72MHz

    TIM_TimeBaseInitTypeDef TIMER_InitStructure;
    RCC_APB1PeriphClockCmd(RCC_APB1Periph_TIM4, ENABLE);
    TIM_TimeBaseStructInit(&TIMER_InitStructure);
    TIMER_InitStructure.TIM_CounterMode = TIM_CounterMode_Up;
    TIMER_InitStructure.TIM_Prescaler = 0;
    TIMER_InitStructure.TIM_Period = TIM_PERIOD;//out 256kHz
		TIM_TimeBaseInit(TIM4, &TIMER_InitStructure);
	  TIM_SelectOutputTrigger(TIM4,TIM_TRGOSource_Update);//The update event is selected as a trigger output
    TIM_Cmd(TIM4, ENABLE);
	  TIM_ARRPreloadConfig(TIM4, ENABLE);//TIM4->CR1 |= TIM_CR1_ARPE;
}
//+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++//
//================================= adc_ini =====================================//
//+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++//
void adc_ini (void){
    ADC_InitTypeDef ADC_InitStructure;
    GPIO_InitTypeDef  GPIO_InitStructure;
    GPIO_InitStructure.GPIO_Mode  = GPIO_Mode_AIN;
    GPIO_InitStructure.GPIO_Pin   = GPIO_Pin_0 ;  
    GPIO_Init(GPIOB, &GPIO_InitStructure);	//PORTB_PIN0
    NVIC_InitTypeDef NVIC_InitStructure;
    NVIC_InitStructure.NVIC_IRQChannel = ADC1_2_IRQn;
    NVIC_InitStructure.NVIC_IRQChannelPreemptionPriority = 0;
    NVIC_InitStructure.NVIC_IRQChannelSubPriority = 0;
    NVIC_InitStructure.NVIC_IRQChannelCmd = ENABLE;
    NVIC_Init(&NVIC_InitStructure);
    RCC_ADCCLKConfig (RCC_PCLK2_Div6);//max 12MHz --> 72/6=12MHz
    RCC_APB2PeriphClockCmd(RCC_APB2Periph_ADC1, ENABLE);
    ADC_InitStructure.ADC_Mode = ADC_Mode_Independent;
    ADC_InitStructure.ADC_ScanConvMode = DISABLE;
    ADC_InitStructure.ADC_ContinuousConvMode = DISABLE;  
    ADC_InitStructure.ADC_ExternalTrigConv = ADC_ExternalTrigConv_None;
    ADC_InitStructure.ADC_DataAlign = ADC_DataAlign_Right;
    ADC_InitStructure.ADC_NbrOfChannel = 1;
    ADC_Init ( ADC1, &ADC_InitStructure); 
	  ADC_InjectedSequencerLengthConfig(ADC1, 1);
    ADC_InjectedChannelConfig(ADC1, ADC_Channel_8, 1,ADC_SampleTime_13Cycles5);//13.5
    ADC_ExternalTrigInjectedConvConfig( ADC1, ADC_ExternalTrigInjecConv_T4_TRGO);//ADC1->CR2 |=  ADC_ExternalTrigInjecConv_T4_TRGO;
		ADC_SetInjectedOffset(ADC1,ADC_InjectedChannel_1,ADC_INJECTED_OFFSET);
    ADC_Cmd (ADC1,ENABLE);  // ADC1->CR2 |= CR2_ADON_Set
    ADC_ResetCalibration(ADC1); 
    while(ADC_GetResetCalibrationStatus(ADC1));
    ADC_StartCalibration(ADC1);
    while(ADC_GetCalibrationStatus(ADC1));
    ADC_ITConfig(ADC1,ADC_IT_JEOC, ENABLE);// ADC1->CR1 |= ADC_CR1_JEOCIE; 
		ADC_ExternalTrigInjectedConvCmd(ADC1, ENABLE);// ADC1->CR2 |= CR2_JEXTTRIG_Set;

}
//+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++//
//================================= led_ini =====================================//
//+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++//
 void led_ini(void){
    GPIO_InitTypeDef  GPIO_InitStructure;
    RCC_APB2PeriphClockCmd(RCC_APB2Periph_GPIOC, ENABLE);
    GPIO_InitStructure.GPIO_Pin = GPIO_Pin_13;
    GPIO_InitStructure.GPIO_Mode = GPIO_Mode_Out_PP;
    GPIO_InitStructure.GPIO_Speed = GPIO_Speed_50MHz;
    GPIO_Init(GPIOC, &GPIO_InitStructure);
    
 }

//+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++//
//============================ ADC1_2_IRQHandler ================================//
//+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++//	
void ADC1_2_IRQHandler(void)
{   
	  
    if(ADC_GetITStatus(ADC1,ADC_IT_JEOC))
    {
				ADC_ClearITPendingBit(ADC1,ADC_IT_JEOC);
				ovs_acc += (int16_t)(ADC1->JDR1); // Get ADC value;
				ovs_cnt++;
			  if (ovs_cnt >= OVS_COUNT) {
					  adc_val = ovs_acc / ovs_cnt;
						adc_buffer[adc_buf_pos++] = (adc_val & 0X00FF) ;
						adc_buffer[adc_buf_pos++] = (adc_val >> 8) ; 
					  adc_buf_pos &= ADC_BUF_MASK;
						ovs_acc = 0;
						ovs_cnt = 0;
		  }			
    }
}


//+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++//
//==================================SWO_ini======================================//
//+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++//
#define ITM_Port8(n)    (*((volatile unsigned char *)(0xE0000000+4*n)))
#define ITM_Port16(n)   (*((volatile unsigned short*)(0xE0000000+4*n)))
#define ITM_Port32(n)   (*((volatile unsigned long *)(0xE0000000+4*n)))
#define DEMCR           (*((volatile unsigned long *)(0xE000EDFC)))
#define TRCENA           0x01000000


struct __FILE { int handle;};
FILE __stdout;
FILE __stdin;

int fputc(int ch, FILE *f) {
   if (DEMCR & TRCENA) {

while (ITM_Port32(0) == 0){};
    ITM_Port8(0) = ch;
  }
  return(ch);
}
char *crcOK;

//++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++//
//================================ perif ini =====================================//
//+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++//
int main(void)
{
  for(uint32_t i = 0;i<10000000;i++){;}
	Set_System();
  SetSysClock();
	Set_USBClock();
  USB_Interrupts_Config();
	USB_Init();
	adc_ini();
	tim4_ini();
	led_ini();
	uint16_t temp; 
	while (bDeviceState != CONFIGURED){;}	
//++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++//
//=================================== while ======================================//
//+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++//
  while (1)
  {
   	if (packet_send) {//EP1_IN_Callback
    		if (_GetENDPOINT(ENDP1) & EP_DTOG_RX) {
        		UserToPMABufferCopy(&adc_buffer[usb_buf_pos],ENDP1_BUF0Addr,USB_SEND_PACKET_SIZE);
        		SetEPDblBuf0Count(ENDP1,EP_DBUF_IN,USB_SEND_PACKET_SIZE);
   		}
					else {
        		UserToPMABufferCopy(&adc_buffer[usb_buf_pos],ENDP1_BUF1Addr,USB_SEND_PACKET_SIZE);
        		SetEPDblBuf1Count(ENDP1,EP_DBUF_IN,USB_SEND_PACKET_SIZE);
    		}
    		FreeUserBuffer(ENDP1,EP_DBUF_IN); // Toggles EP_DTOG_RX
    		SetEPTxValid(ENDP1);
    		packet_send = 0;

    		usb_buf_pos += USB_SEND_PACKET_SIZE;//wMaxPacketSize: 64 bytes per packet
    		if (usb_buf_pos > ADC_BUF_SIZE - USB_SEND_PACKET_SIZE) usb_buf_pos = 0;
    	}
		 if(adc_buf_pos > usb_buf_pos){
			  temp = adc_buf_pos - usb_buf_pos;
			  if(temp>512)TIM4->ARR = TIM_PERIOD + 1;
		    else TIM4->ARR = TIM_PERIOD;
		 }
     if (adc_val > SILENCE_LEVEL)GPIO_ResetBits(GPIOC, GPIO_Pin_13); 
			else GPIO_SetBits(GPIOC, GPIO_Pin_13);  
  }
}

