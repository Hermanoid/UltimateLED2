using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.Devices.Gpio;
using System.Text;
using Windows.UI;
using Microsoft.Azure.Devices.Client;
using System.Threading.Tasks;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace UltimateLED2
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        const string DeviceId = "TheBoss";
        const string DeviceKey = "pj3ZTs+vNlsDy01iutTXLYsNgoRLOwgCvV088uiQ2wY=";
        const string HubEndpoint = "starter.azure-devices.net";
        const int LEDPinNumber = 5;
        GpioPin LEDPin;
        bool LEDPinState;
        Brush StatusNormalBrush;
        DeviceClient deviceClient;

        public MainPage()
        {
            this.InitializeComponent();
            StatusNormalBrush = StatusIndicator.Fill;
            if (!TryInitGPIO().Result)
            {
                WriteMessage("GPIO initialization failed").Wait();
            }
            deviceClient = DeviceClient.Create(HubEndpoint,
                AuthenticationMethodFactory.CreateAuthenticationWithRegistrySymmetricKey(DeviceId, DeviceKey), TransportType.Mqtt_WebSocket_Only);
            deviceClient.SetMethodHandlerAsync("ToggleLED", new MethodCallback(ToggleLEDMethod), null);
        }

        private async Task<MethodResponse> ToggleLEDMethod(MethodRequest methodRequest, object userContext)
        {
            await WriteMessage("Recieved Direct Request to toggle LED");
            LEDPinState = !LEDPinState;
            await UpdateLight();
            return new MethodResponse(Encoding.UTF8.GetBytes("{\"LightIs\":\"" + (LEDPinState ? "On" : "Off") + "\"}"), 200);
        }

        public async Task<bool> TryInitGPIO()
        {
            GpioController gpioController = GpioController.GetDefault();
            if (gpioController == null)
            {
                await WriteMessage("This Device is not IoT friendly!  (No GPIO Controller found)", true);
                return false;
            }
            if (gpioController.TryOpenPin(LEDPinNumber, GpioSharingMode.Exclusive, out LEDPin, out GpioOpenStatus openStatus))
            {
                await WriteMessage($"Output Pin ({LEDPinNumber}) Opened Successfully!!");
            }
            else
            {
                await WriteMessage($"Output Pin ({LEDPinNumber}) Failed to Open", true);
                return false;
            }

            LEDPin.SetDriveMode(GpioPinDriveMode.Output);
            LEDPin.Write(GpioPinValue.High);
            LEDPinState = true;
            await UpdateLight();
            await WriteMessage("Output Pin initialized and on");
            return true;
        }

        private async Task WriteMessage(string message, bool isError = false)
        {
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
              {
                  StringBuilder sb = new StringBuilder(OutputBox.Text);
                  if (isError)
                  {
                      sb.AppendLine();
                      sb.AppendLine("*************ERROR**************");
                  }
                  sb.AppendLine(message);
                  if (isError)
                  {
                      sb.AppendLine("*************END ERROR**************");
                      sb.AppendLine();
                  }
                  OutputBox.Text = sb.ToString();
              });
            
        }

        private async void ManualToggle_Click(object sender, RoutedEventArgs e)
        {
            await WriteMessage("Recieved Manual Toggle");
            LEDPinState = !LEDPinState;
            await UpdateLight();
        }

        private async Task UpdateLight()
        {

            LEDPin.Write(LEDPinState ? GpioPinValue.High : GpioPinValue.Low);
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
             {
                 StatusIndicator.Fill = LEDPinState ? new SolidColorBrush(Colors.Red) : StatusNormalBrush;
             });


        }
    }
}
