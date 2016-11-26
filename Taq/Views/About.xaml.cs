﻿using System;
using Windows.ApplicationModel;
using Windows.UI.Xaml.Controls;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Taq.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class About : Page
    {

        public App app;
        public About()
        {
            app = App.Current as App;
            this.InitializeComponent();
            this.DataContext = this;
        }

        public string mailUri
        {
            get
            {
                return "mailto:myhDev@live.com?subject=問題回報&body=TAQ版本：" + app.version;
            }
        }
    }
}
