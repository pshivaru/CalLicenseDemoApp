﻿using CalLicenseDemo.Logic;
using CalLicenseDemo.ViewModel;
using CalLicenseDemo.Views;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace CalLicenseDemo
{
    /// <summary>
    /// Interaction logic for CreditAndDebitCardDetails.xaml
    /// </summary>
    public partial class CreditAndDebitCardDetails : Page
    {
        /// <summary>
        /// Constructor initialization of data
        /// </summary>
        public CreditAndDebitCardDetails()
        {
            InitializeComponent();
            var viewModel = new CreditAndDebitCardDetailsViewModel();
            DataContext = viewModel;
            viewModel.NavigateNextPage += OnNavigateNextPage;
        }

        /// <summary>
        /// Navigation to diffrent page action
        /// </summary>
        /// <param name="screenName">screenName</param>
        /// <param name="additionalInfo">additionalInfo</param>
        private void OnNavigateNextPage(string screenName, Dictionary<string,string> additionalInfo)
        {
            this.NavigationService.Navigate(new RedirectToAmountPaymentPage());
        }

    }
}
