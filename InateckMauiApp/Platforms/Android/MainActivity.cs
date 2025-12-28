using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Util;
using AW = Android.Widget;

namespace InateckMauiApp;

/// <summary>
/// Main Menu Activity - Entry point for selecting scanner mode.
/// Allows switching between HID mode (keyboard) and SDK mode (BLE commands).
/// </summary>
[Activity(
    Label = "Inatek Scanner",
    Theme = "@style/Maui.SplashTheme",
    MainLauncher = true,
    ConfigurationChanges = ConfigChanges.ScreenSize |
                          ConfigChanges.Orientation |
                          ConfigChanges.UiMode |
                          ConfigChanges.ScreenLayout |
                          ConfigChanges.SmallestScreenSize |
                          ConfigChanges.Density)]
public class MainActivity : Activity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        
        Log.Info("InateckScanner", "[Menu] MainActivity OnCreate");
        
        // Create main layout
        var mainLayout = new AW.LinearLayout(this)
        {
            Orientation = AW.Orientation.Vertical
        };
        mainLayout.SetPadding(40, 60, 40, 40);
        mainLayout.SetBackgroundColor(Android.Graphics.Color.White);
        mainLayout.SetGravity(Android.Views.GravityFlags.Center);
        
        // Title
        var titleText = new AW.TextView(this)
        {
            Text = "📱 Inatek Scanner",
            TextSize = 28,
            Gravity = Android.Views.GravityFlags.Center
        };
        titleText.SetTextColor(Android.Graphics.Color.Black);
        titleText.SetTypeface(null, Android.Graphics.TypefaceStyle.Bold);
        var titleParams = new AW.LinearLayout.LayoutParams(
            AW.LinearLayout.LayoutParams.MatchParent,
            AW.LinearLayout.LayoutParams.WrapContent);
        titleParams.SetMargins(0, 0, 0, 20);
        mainLayout.AddView(titleText, titleParams);
        
        // Subtitle
        var subtitleText = new AW.TextView(this)
        {
            Text = "Select Scanner Mode",
            TextSize = 16,
            Gravity = Android.Views.GravityFlags.Center
        };
        subtitleText.SetTextColor(Android.Graphics.Color.Gray);
        var subtitleParams = new AW.LinearLayout.LayoutParams(
            AW.LinearLayout.LayoutParams.MatchParent,
            AW.LinearLayout.LayoutParams.WrapContent);
        subtitleParams.SetMargins(0, 0, 0, 60);
        mainLayout.AddView(subtitleText, subtitleParams);
        
        // ========================================
        // HID Mode Button (Working)
        // ========================================
        var hidButton = new AW.Button(this)
        {
            Text = "🎮 HID MODE\n(Keyboard Emulation)"
        };
        hidButton.SetTextColor(Android.Graphics.Color.White);
        hidButton.SetBackgroundColor(Android.Graphics.Color.ParseColor("#4CAF50")); // Green
        hidButton.SetPadding(20, 30, 20, 30);
        hidButton.Click += (s, e) => StartHidMode();
        var hidParams = new AW.LinearLayout.LayoutParams(
            AW.LinearLayout.LayoutParams.MatchParent,
            AW.LinearLayout.LayoutParams.WrapContent);
        hidParams.SetMargins(0, 0, 0, 20);
        mainLayout.AddView(hidButton, hidParams);
        
        // HID Mode description
        var hidDescText = new AW.TextView(this)
        {
            Text = "✓ Working - Scanner sends barcodes as keyboard input.\nNo SDK commands, uses standard BLE for battery/version.",
            TextSize = 12,
            Gravity = Android.Views.GravityFlags.Center
        };
        hidDescText.SetTextColor(Android.Graphics.Color.DarkGray);
        var hidDescParams = new AW.LinearLayout.LayoutParams(
            AW.LinearLayout.LayoutParams.MatchParent,
            AW.LinearLayout.LayoutParams.WrapContent);
        hidDescParams.SetMargins(0, 0, 0, 40);
        mainLayout.AddView(hidDescText, hidDescParams);
        
        // ========================================
        // SDK Mode Button (Experimental)
        // ========================================
        var sdkButton = new AW.Button(this)
        {
            Text = "📡 SDK MODE\n(BLE Commands)"
        };
        sdkButton.SetTextColor(Android.Graphics.Color.White);
        sdkButton.SetBackgroundColor(Android.Graphics.Color.ParseColor("#2196F3")); // Blue
        sdkButton.SetPadding(20, 30, 20, 30);
        sdkButton.Click += (s, e) => StartSdkMode();
        var sdkParams = new AW.LinearLayout.LayoutParams(
            AW.LinearLayout.LayoutParams.MatchParent,
            AW.LinearLayout.LayoutParams.WrapContent);
        sdkParams.SetMargins(0, 0, 0, 20);
        mainLayout.AddView(sdkButton, sdkParams);
        
        // SDK Mode description
        var sdkDescText = new AW.TextView(this)
        {
            Text = "🔨 Experimental - Uses Inatek SDK BLE commands.\nAttempts to control scanner via SDK API.",
            TextSize = 12,
            Gravity = Android.Views.GravityFlags.Center
        };
        sdkDescText.SetTextColor(Android.Graphics.Color.DarkGray);
        var sdkDescParams = new AW.LinearLayout.LayoutParams(
            AW.LinearLayout.LayoutParams.MatchParent,
            AW.LinearLayout.LayoutParams.WrapContent);
        sdkDescParams.SetMargins(0, 0, 0, 40);
        mainLayout.AddView(sdkDescText, sdkDescParams);
        
        // Version info
        var versionText = new AW.TextView(this)
        {
            Text = "v1.0.0 - BCST-75S",
            TextSize = 11,
            Gravity = Android.Views.GravityFlags.Center
        };
        versionText.SetTextColor(Android.Graphics.Color.LightGray);
        mainLayout.AddView(versionText);
        
        SetContentView(mainLayout);
    }
    
    private void StartHidMode()
    {
        Log.Info("InateckScanner", "[Menu] Starting HID Mode...");
        var intent = new Intent(this, typeof(HidScannerActivity));
        StartActivity(intent);
    }
    
    private void StartSdkMode()
    {
        Log.Info("InateckScanner", "[Menu] Starting SDK Mode...");
        var intent = new Intent(this, typeof(SdkScannerActivity));
        StartActivity(intent);
    }
}
