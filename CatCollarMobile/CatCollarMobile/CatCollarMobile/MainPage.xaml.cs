using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace CatCollarMobile
{
    public partial class MainPage : ContentPage
    {
        bool active = true;
        static HttpClient client = new HttpClient();
        public MainPage()
        {
            InitializeComponent();

            Collars.SelectedIndexChanged += When_SelectedIndexChanged;
            ApdateList();
        }

        private async void ApdateList()
        {
            while (true)
            {
                HttpResponseMessage response = await client.GetAsync("http://10.0.2.2:5000/devices");

                if (response.IsSuccessStatusCode)
                {
                    var valuesArray = JArray.Parse(await response.Content.ReadAsStringAsync());
                    foreach (JObject root in valuesArray)
                    {
                        foreach (KeyValuePair<String, JToken> app in root)
                        {
                            Collars.Items.Add((string)app.Value);
                        }
                    }
                }
                if (Collars.Items.Count == 0)
                {
                    Collars.SelectedItem = null;
                }
                await Task.Delay(30000);
            }
        }

        private void When_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (active == true && Collars.SelectedItem.Equals(null))
            {
                Result.Text = "Choose collar";
                active = false;
            }
            else
            {
                active = true;
                DisplayResult(Collars.SelectedItem.ToString());
            }
        }

        private async void DisplayResult(string selectedItem)
        {
            while (active)
            {
                HttpResponseMessage response = await client.GetAsync($"http://10.0.2.2:5000/Audio/{selectedItem}");

                var value= JObject.Parse(await response.Content.ReadAsStringAsync());
                ColorTypeConverter colorConverter = new ColorTypeConverter();

                Result.Text = (string)value["result"];
                Result.TextColor = (Color)colorConverter.ConvertFromInvariantString((string)value["color"]);

                await Task.Delay(100);
            }
        }
    }
}
