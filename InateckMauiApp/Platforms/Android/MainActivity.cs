using Android;
using Android.App;
using Android.Content.PM;
using Android.OS;
using AndroidX.Core.App;
using AndroidX.Core.Content;

namespace InateckMauiApp;

[Activity(
    Theme = "@style/Maui.SplashTheme",
    MainLauncher = true,
    ConfigurationChanges = ConfigChanges.ScreenSize |
                          ConfigChanges.Orientation |
                          ConfigChanges.UiMode |
                          ConfigChanges.ScreenLayout |
                          ConfigChanges.SmallestScreenSize |
                          ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
    private const int PERMISSION_REQUEST_CODE = 1001;

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

        // Solicitar permisos según la versión de Android
        RequestNecessaryPermissions();
    }

    private void RequestNecessaryPermissions()
    {
        var permissionsToRequest = new List<string>();

        // =====================================================
        // PERMISOS PARA ANDROID 12+ (API 31+)
        // =====================================================
        if (Build.VERSION.SdkInt >= BuildVersionCodes.S) // Android 12+
        {
            // Bluetooth Scan
            if (ContextCompat.CheckSelfPermission(this, Manifest.Permission.BluetoothScan) != Permission.Granted)
            {
                permissionsToRequest.Add(Manifest.Permission.BluetoothScan);
            }

            // Bluetooth Connect
            if (ContextCompat.CheckSelfPermission(this, Manifest.Permission.BluetoothConnect) != Permission.Granted)
            {
                permissionsToRequest.Add(Manifest.Permission.BluetoothConnect);
            }

            // Bluetooth Advertise (opcional, solo si se anuncia como periférico)
            if (ContextCompat.CheckSelfPermission(this, Manifest.Permission.BluetoothAdvertise) != Permission.Granted)
            {
                permissionsToRequest.Add(Manifest.Permission.BluetoothAdvertise);
            }
        }

        // =====================================================
        // PERMISOS DE LOCALIZACIÓN (Requeridos para BLE)
        // =====================================================
        if (ContextCompat.CheckSelfPermission(this, Manifest.Permission.AccessFineLocation) != Permission.Granted)
        {
            permissionsToRequest.Add(Manifest.Permission.AccessFineLocation);
        }

        if (ContextCompat.CheckSelfPermission(this, Manifest.Permission.AccessCoarseLocation) != Permission.Granted)
        {
            permissionsToRequest.Add(Manifest.Permission.AccessCoarseLocation);
        }

        // =====================================================
        // SOLICITAR PERMISOS SI ES NECESARIO
        // =====================================================
        if (permissionsToRequest.Count > 0)
        {
            ActivityCompat.RequestPermissions(
                this,
                permissionsToRequest.ToArray(),
                PERMISSION_REQUEST_CODE
            );
        }
    }

    public override void OnRequestPermissionsResult(
        int requestCode,
        string[] permissions,
        Permission[] grantResults)
    {
        base.OnRequestPermissionsResult(requestCode, permissions, grantResults);

        if (requestCode == PERMISSION_REQUEST_CODE)
        {
            var allGranted = true;
            var deniedPermissions = new List<string>();

            for (int i = 0; i < permissions.Length; i++)
            {
                if (grantResults[i] != Permission.Granted)
                {
                    allGranted = false;
                    deniedPermissions.Add(permissions[i]);
                }
            }

            if (allGranted)
            {
                System.Diagnostics.Debug.WriteLine("✅ Todos los permisos fueron otorgados");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"⚠️ Permisos denegados: {string.Join(", ", deniedPermissions)}");

                // Mostrar diálogo explicativo si es necesario
                ShowPermissionExplanationIfNeeded(deniedPermissions);
            }
        }
    }

    private void ShowPermissionExplanationIfNeeded(List<string> deniedPermissions)
    {
        // Verificar si debemos mostrar una explicación
        var shouldShowRationale = deniedPermissions.Any(permission =>
            ActivityCompat.ShouldShowRequestPermissionRationale(this, permission)
        );

        if (shouldShowRationale)
        {
            var builder = new AndroidX.AppCompat.App.AlertDialog.Builder(this);
            builder.SetTitle("Permisos requeridos");
            builder.SetMessage(
                "Esta aplicación requiere permisos de Bluetooth y Ubicación para " +
                "escanear y conectarse al escáner Inatek.\n\n" +
                "Sin estos permisos, la aplicación no podrá funcionar correctamente."
            );
            builder.SetPositiveButton("Reintentar", (sender, args) =>
            {
                RequestNecessaryPermissions();
            });
            builder.SetNegativeButton("Cancelar", (sender, args) =>
            {
                System.Diagnostics.Debug.WriteLine("⚠️ Usuario canceló la solicitud de permisos");
            });
            builder.Show();
        }
    }
}
