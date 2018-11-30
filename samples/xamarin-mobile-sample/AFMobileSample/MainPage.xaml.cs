using System;
using AgentFramework.Core.Contracts;
using AgentFramework.Core.Models.Wallets;
using Autofac;
using Hyperledger.Indy.DidApi;
using Hyperledger.Indy.WalletApi;
using Xamarin.Forms;

namespace AFMobileSample
{
    public partial class MainPage : ContentPage
    {
        private readonly IWalletService _walletService = App.Container.Resolve<IWalletService>();
        private readonly IConnectionService _connectionService = App.Container.Resolve<IConnectionService>();

        private WalletConfiguration _config = new WalletConfiguration { Id = "MyWallet" };
        private WalletCredentials _creds = new WalletCredentials { Key = "SecretKey" };
        private Wallet _wallet;

        public MainPage()
        {
            InitializeComponent();
        }

        async void OnCreateClicked(object sender, EventArgs e)
        {
            ProgressIndicator.IsRunning = true;
            CreateButton.IsEnabled = false;

            try
            {
                await _walletService.CreateWalletAsync(_config, _creds);
            }
            catch (WalletExistsException)
            {
                //
            }

            _wallet = _wallet ?? await _walletService.GetWalletAsync(_config, _creds);
            var did = await Did.CreateAndStoreMyDidAsync(_wallet, "{}");

            Device.BeginInvokeOnMainThread(() =>
            {
                DidLabel.Text = $"Identity created -> Did: {did.Did}";

                ProgressIndicator.IsRunning = false;
                CreateButton.IsEnabled = true;
            });
        }
    }
}
