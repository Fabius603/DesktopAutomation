using ControlzEx.Theming;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace DesktopAutomationApp.Services
{
    public interface IThemeService
    {
        void UseDarkOrange();
        void UseMonochrome(); // Schwarz-Weiß
    }

    public sealed class ThemeService : IThemeService
    {
        public void UseDarkOrange()
        {
            // Fertiges MahApps-Theme
            ThemeManager.Current.ChangeTheme(Application.Current, "Dark.Orange");
        }

        public void UseMonochrome()
        {
            // Monochrom: neutrales, helles Theme als Basis
            ThemeManager.Current.ChangeTheme(Application.Current, "Light.Steel"); // graue Akzente

            // Optional: noch „schwärzer/weißer“ machen
            // Kleine Overrides per MergedDictionary hinzufügen/entfernen:
            var uri = new Uri("pack://application:,,,/DesktopAutomationApp;component/Themes/MonochromeOverrides.xaml");
            var dict = new ResourceDictionary { Source = uri };

            // entfernen, falls schon geladen
            var appDicts = Application.Current.Resources.MergedDictionaries;
            for (int i = appDicts.Count - 1; i >= 0; i--)
                if (appDicts[i].Source == uri) appDicts.RemoveAt(i);

            appDicts.Add(dict);
        }
    }
}
