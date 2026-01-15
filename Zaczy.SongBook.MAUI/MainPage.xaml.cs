using System;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;

namespace Zaczy.SongBook.MAUI
{
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();
        }

        // Existing example handler (preserved)
        private int count = 0;
        private void OnCounterClicked(object sender, EventArgs e)
        {
            count++;
            CounterBtn.Text = $"Clicked {count} time{(count == 1 ? "" : "s")}";
        }

        // Pobierz zestaw -> tries to call LoadSongsAsync() on BindingContext if present
        private async void OnFetchSetClicked(object sender, EventArgs e)
        {
            if (BindingContext == null)
            {
                await DisplayAlert("Info", "No BindingContext set.", "OK");
                return;
            }

            var bcType = BindingContext.GetType();
            var loadMethod = bcType.GetMethod("LoadSongsAsync", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            if (loadMethod != null)
            {
                var result = loadMethod.Invoke(BindingContext, null);
                if (result is Task task)
                {
                    try
                    {
                        await task;
                    }
                    catch (Exception ex)
                    {
                        await DisplayAlert("Error", $"LoadSongsAsync failed: {ex.Message}", "OK");
                    }
                }
                else
                {
                    await DisplayAlert("Info", "LoadSongsAsync invoked (non-async method).", "OK");
                }

                return;
            }

            await DisplayAlert("Info", "BindingContext does not expose LoadSongsAsync().", "OK");
        }

        // Tonacja + -> tries to call AdjustTonation(int) on BindingContext if present
        private async void OnToneUpClicked(object sender, EventArgs e)
        {
            await InvokeAdjustTonationIfExists(1);
        }

        // Tonacja - -> tries to call AdjustTonation(int) on BindingContext if present
        private async void OnToneDownClicked(object sender, EventArgs e)
        {
            await InvokeAdjustTonationIfExists(-1);
        }

        private async Task InvokeAdjustTonationIfExists(int delta)
        {
            if (BindingContext == null)
            {
                await DisplayAlert("Info", "No BindingContext set.", "OK");
                return;
            }

            var bcType = BindingContext.GetType();
            var adjustMethod = bcType.GetMethod("AdjustTonation", new Type[] { typeof(int) });

            if (adjustMethod != null)
            {
                try
                {
                    var result = adjustMethod.Invoke(BindingContext, new object[] { delta });
                    if (result is Task t)
                        await t;
                    return;
                }
                catch (TargetInvocationException tie)
                {
                    await DisplayAlert("Error", $"AdjustTonation threw: {tie.InnerException?.Message ?? tie.Message}", "OK");
                    return;
                }
            }

            // Fallback: try ICommand properties named ToneUpCommand / ToneDownCommand
            string commandName = delta > 0 ? "ToneUpCommand" : "ToneDownCommand";
            var cmdProp = bcType.GetProperty(commandName);
            if (cmdProp != null)
            {
                var cmd = cmdProp.GetValue(BindingContext) as System.Windows.Input.ICommand;
                if (cmd != null && cmd.CanExecute(null))
                {
                    cmd.Execute(null);
                    return;
                }
            }

            await DisplayAlert("Info", "BindingContext does not expose AdjustTonation(int) or appropriate command.", "OK");
        }
    }
}
