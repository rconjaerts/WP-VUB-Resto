using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using RestaurantVUB.Resources;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Media;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace RestaurantVUB
{
    public partial class MainPage : PhoneApplicationPage
    {
        List<DateTime> dates = new List<DateTime>();
        readonly ApplicationBar appBar;
        String currentUri;

        // Constructor
        public MainPage()
        {
            InitializeComponent();
            for (int i = 0; i < 7; i++)
            {
                dates.Add(DateTime.Now.AddDays(i));
            }
            currentUri = AppResources.LinkMenuEtterbeek;
            loadData(currentUri);
            pivot.Title = "VUB Resto: Etterbeek";

            appBar = new ApplicationBar();
            appBar.IsMenuEnabled = false;
            
            createAppBarButton(AppResources.switchResto);
        }

        private void createAppBarButton(String text)
        {
            appBar.Buttons.Clear();
            ApplicationBarIconButton button = new ApplicationBarIconButton();
            button.IconUri = new Uri("/Resources/appbar.arrow.left.right.png", UriKind.Relative);
            button.Text = text;
            appBar.Buttons.Add(button);

            button.Click += new EventHandler(changeRestaurant);
            ApplicationBar = appBar;
        }

        private void changeRestaurant(object sender, EventArgs e)
        {
            pivot.Items.Clear();
            if (currentUri.Equals(AppResources.LinkMenuEtterbeek))
            {
                currentUri = AppResources.LinkMenuJette;
                pivot.Title = "VUB Resto: Jette";
            }
            else
            {
                currentUri = AppResources.LinkMenuEtterbeek;
                pivot.Title = "VUB Resto: Etterbeek";
            }
            loadData(currentUri);
        }

        async private void loadData(String uri)   
        {
            try
            {
                progressBar1.IsIndeterminate = true;
                progressBar1.Visibility = Visibility.Visible;
                var client = new HttpClient();
                var response = await client.GetAsync(new Uri(uri));
                var result = await response.Content.ReadAsStringAsync();

                this.Dispatcher.BeginInvoke(delegate()
                {
                    buildPivotsAndPopulate(result);
                });
            }
            catch (Exception)
            {
                addErrorPage();
            }
        }

        private void buildPivotsAndPopulate(string json)
        {
            try
            {
                JArray days = JArray.Parse(json);

                if (days.Count != 0)
                {
                    JToken daysItem = days.First;

                    foreach (DateTime datum in dates)
                    {
                        if ((int)datum.DayOfWeek == 6 || (int)datum.DayOfWeek == 0)
                        {
                            pivot.Items.Add(createPage(datum, AppResources.ClosedOnWeekend));
                        }
                        else if (daysItem != null)
                        {
                            string date = daysItem.Value<string>("date").Replace("-", "/");
                            DateTime dt = Convert.ToDateTime(date);

                            while (dt.DayOfYear.CompareTo(datum.DayOfYear) < 0)
                            {
                                daysItem = daysItem.Next;
                            }

                            PivotItem pvt = new PivotItem();
                            pvt.Header = datum.ToString("ddd dd");

                            if (dt.DayOfYear == datum.DayOfYear)
                            {
                                ObservableCollection<Menu> items = new ObservableCollection<Menu>();

                                JArray menus = daysItem.Value<JArray>("menus");
                                JToken menusItem = menus.First;
                                while (menusItem != null)
                                {
                                    Menu menu = JsonConvert.DeserializeObject<Menu>(menusItem.ToString());
                                    items.Add(menu);

                                    menusItem = menusItem.Next;
                                }

                                VUBListItem myControl = new VUBListItem();
                                myControl.DataContext = new Menu();
                                myControl.longList.ItemsSource = items;
                                pvt.Content = myControl;
                                pivot.Items.Add(pvt);
                            }
                            else
                            {
                                pivot.Items.Add(createPage(datum, AppResources.NoMeals));
                            }
                            daysItem = daysItem.Next;
                        }
                        else
                        {
                            pivot.Items.Add(createPage(datum, AppResources.NoMeals));
                            if(daysItem != null)
                                daysItem = daysItem.Next;
                        }
                    }
                }
                else
                {
                    pivot.Items.Add(createPage(DateTime.Now, AppResources.NoMeals));
                }
            }
            catch (Exception)
            {
                addErrorPage();
            }
            progressBar1.IsIndeterminate = false;
            progressBar1.Visibility = Visibility.Collapsed;
        }

        private PivotItem createPage(DateTime datum, String pageText)
        {
            PivotItem pvt = new PivotItem();
            pvt.Header = datum.ToString("ddd dd");

            TextBlock text = new TextBlock();
            text.Text = pageText;
            text.TextAlignment = TextAlignment.Center;
            text.TextWrapping = TextWrapping.Wrap;
            text.FontSize = 30;
            text.Foreground = Application.Current.Resources["backgroundText"] as SolidColorBrush;
            Grid grid = new Grid();
            grid.Children.Add(text);
            pvt.Content = grid;

            return pvt;
        }

        private void addErrorPage()
        {
            progressBar1.IsIndeterminate = false;
            progressBar1.Visibility = Visibility.Collapsed;
            PivotItem pvt = new PivotItem();
            pvt.Header = DateTime.Now.ToString("ddd dd");

            TextBlock text = new TextBlock()
            {
                Text = AppResources.CantLoadInfo,
                TextAlignment = TextAlignment.Center,
                FontSize = 30,
                TextWrapping = TextWrapping.Wrap,
                Foreground = new SolidColorBrush(Colors.Gray),
            };

            Button btnRetry = new Button()
            {
                Content = AppResources.Retry,
                Foreground = new SolidColorBrush(Colors.Gray),
                BorderBrush = new SolidColorBrush(Colors.Gray),
            };

            btnRetry.Click += retryContentLoad;

            Grid grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto });
            grid.Children.Add(text);
            grid.Children.Add(btnRetry);
            Grid.SetRow(text, 0);
            Grid.SetRow(btnRetry, 1);

            pvt.Content = grid;
            pivot.Items.Add(pvt);
        }

        private void retryContentLoad(object sender, EventArgs e)
        {
            pivot.Items.Clear();
            loadData(currentUri);
        }

    }
}