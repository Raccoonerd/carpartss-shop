using CarPartsStore.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Text;

namespace CarPartsStore.ViewModels
{
    internal partial class CzesciViewModel : ObservableObject
    {
        [ObservableProperty]
        List<Czesci> m_Czesci;

        public CzesciViewModel ()
        {
            using var db = new CarPartsShopContext();
            m_Czesci = db.Czesci.ToList();
        }
    }
}
