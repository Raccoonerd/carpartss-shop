using CarPartsStore.Views.Dialogs;
using MaterialDesignThemes.Wpf;
using System;
using System.Collections.Generic;
using System.Text;

namespace CarPartsStore.Services
{
    internal class DialogService
    {
        private const string HostIdentifier = "MainDialogHost";

        public static async Task ShowInfoAsync(string title, string message)
        {
            var dialog = new InfoDialog(title, message);
            await DialogHost.Show(dialog, HostIdentifier);
        }

        public static async Task ShowErrorAsync(string title, string message)
        {
            var dialog = new ErrorDialog(title, message);
            await DialogHost.Show(dialog, HostIdentifier);
        }

        public static async Task<bool> ShowConfirmAsync(string title, string message)
        {
            var dialog = new ConfirmDialog(title, message);
            var result = await DialogHost.Show(dialog, HostIdentifier);
            return result is true;
        }
    }
}
