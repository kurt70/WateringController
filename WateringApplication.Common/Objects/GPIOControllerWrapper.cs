using Microsoft.IoT.Lightning.Providers;
using System;
using Windows.Devices;
using Windows.Devices.Gpio;
using Windows.Foundation.Metadata;

namespace WateringApplication.Common.Objects
{
    public interface IGPIOControllerWrapper
    {
        GpioController Controller { get; }

        #region IGPIOController
        //
        // Summary:
        //     Opens a connection to the specified general-purpose I/O (GPIO) pin in exclusive
        //     mode.
        //
        // Parameters:
        //   pinNumber:
        //     The pin number of the GPIO pin that you want to open. The pin number must be
        //
        // Returns:
        //     The opened GPIO pin.
        GpioPin OpenPin(int pinNumber);
        //
        // Summary:
        //     Opens the specified general-purpose I/O (GPIO) pin in the specified mode.
        //
        // Parameters:
        //   pinNumber:
        //     The pin number of the GPIO pin that you want to open. The pin number must be
        //
        //   sharingMode:
        //     The mode in which you want to open the GPIO pin, which determines whether other
        //     connections to the pin can be opened while you have the pin open.
        //
        // Returns:
        //     The opened GPIO pin.

        GpioPin OpenPin(int pinNumber, GpioSharingMode sharingMode);
        //
        // Summary:
        //     Opens the specified general-purpose I/O (GPIO) pin in the specified mode, and
        //     gets a status value that you can use to handle a failure to open the pin programmatically.
        //
        // Parameters:
        //   pinNumber:
        //     The pin number of the GPIO pin that you want to open. Some pins may not be available
        //     in user mode. For information about how the pin numbers correspond to physical
        //     pins, see the documentation for your circuit board.
        //
        //   sharingMode:
        //     The mode in which you want to open the GPIO pin, which determines whether other
        //     connections to the pin can be opened while you have the pin open.
        //
        //   pin:
        //     The opened GPIO pin if the return value is true; otherwise null.
        //
        //   openStatus:
        //     An enumeration value that indicates either that the attempt to open the GPIO
        //     pin succeeded, or the reason that the attempt to open the GPIO pin failed.
        //
        // Returns:
        //     True if the method successfully opened the pin; otherwise false.
        bool TryOpenPin(int pinNumber, GpioSharingMode sharingMode, out GpioPin pin, out GpioOpenStatus openStatus);
        //
        // Summary:
        //     Gets all the controllers that are connected to the system asynchronously.
        //
        // Parameters:
        //   provider:
        //     The GPIO provider for the controllers on the system.
        //
        // Returns:
        //     When the method completes successfully, it returns a list of values that represent
        //     the controllers available on the system.

        //IAsyncOperation<IReadOnlyList<GpioController>> GetControllersAsync(IGpioProvider provider);
        //
        // Summary:
        //     Gets the default general-purpose I/O (GPIO) controller for the system.
        //
        // Returns:
        //     The default GPIO controller for the system, or null if the system has no GPIO
        //     controller.

        //IAsyncOperation<GpioController> GetDefaultAsync();
        //
        // Summary:
        //     Gets the default general-purpose I/O (GPIO) controller for the system.
        //
        // Returns:
        //     The default GPIO controller for the system, or null if the system has no GPIO
        //     controller.
        //GpioController GetDefault();

        //
        // Summary:
        //     Gets the number of pins on the general-purpose I/O (GPIO) controller.
        //
        // Returns:
        //     The number of pins on the GPIO controller. Some pins may not be available in
        //     user mode. For information about how the pin numbers correspond to physical pins,
        //     see the documentation for your circuit board.
        //int PinCount { get; }
        #endregion
    }

    public class GPIOControllerWrapper : IGPIOControllerWrapper
    {
        GpioController _controller;
        public GpioController Controller
        {
            get
            {
                if (_controller == null)
                    _controller = GetController();
                return _controller;
            }
        }

        public GPIOControllerWrapper()
        {
        }
        public static GpioController GetController()
        {
            if (LightningProvider.IsLightningEnabled)
            {
                LowLevelDevicesController.DefaultProvider = LightningProvider.GetAggregateProvider();
            }

            // Remaining initialization code works the same regardless of the current default provider


            var controller = GpioController.GetDefault();

            // Show an error if there is no GPIO controller
            if (controller == null)
            {
                throw new Exception("There is no GPIO controller on this device.");
            }
            return controller;
        }

        #region IGPIOController
        //
        // Summary:
        //     Opens a connection to the specified general-purpose I/O (GPIO) pin in exclusive
        //     mode.
        //
        // Parameters:
        //   pinNumber:
        //     The pin number of the GPIO pin that you want to open. The pin number must be
        //
        // Returns:
        //     The opened GPIO pin.
        [Overload("OpenPin")]
        public GpioPin OpenPin(int pinNumber)
        {
            return Controller.OpenPin(pinNumber);
        }
        //
        // Summary:
        //     Opens the specified general-purpose I/O (GPIO) pin in the specified mode.
        //
        // Parameters:
        //   pinNumber:
        //     The pin number of the GPIO pin that you want to open. The pin number must be
        //
        //   sharingMode:
        //     The mode in which you want to open the GPIO pin, which determines whether other
        //     connections to the pin can be opened while you have the pin open.
        //
        // Returns:
        //     The opened GPIO pin.
        [Overload("OpenPinWithSharingMode")]
        public GpioPin OpenPin(int pinNumber, GpioSharingMode sharingMode)
        {
            return Controller.OpenPin(pinNumber, sharingMode);
        }
        //
        // Summary:
        //     Opens the specified general-purpose I/O (GPIO) pin in the specified mode, and
        //     gets a status value that you can use to handle a failure to open the pin programmatically.
        //
        // Parameters:
        //   pinNumber:
        //     The pin number of the GPIO pin that you want to open. Some pins may not be available
        //     in user mode. For information about how the pin numbers correspond to physical
        //     pins, see the documentation for your circuit board.
        //
        //   sharingMode:
        //     The mode in which you want to open the GPIO pin, which determines whether other
        //     connections to the pin can be opened while you have the pin open.
        //
        //   pin:
        //     The opened GPIO pin if the return value is true; otherwise null.
        //
        //   openStatus:
        //     An enumeration value that indicates either that the attempt to open the GPIO
        //     pin succeeded, or the reason that the attempt to open the GPIO pin failed.
        //
        // Returns:
        //     True if the method successfully opened the pin; otherwise false.
        public bool TryOpenPin(int pinNumber, GpioSharingMode sharingMode, out GpioPin pin, out GpioOpenStatus openStatus)
        {
            return Controller.TryOpenPin(pinNumber, sharingMode, out pin, out openStatus);
        }
        //
        // Summary:
        //     Gets all the controllers that are connected to the system asynchronously.
        //
        // Parameters:
        //   provider:
        //     The GPIO provider for the controllers on the system.
        //
        // Returns:
        //     When the method completes successfully, it returns a list of values that represent
        //     the controllers available on the system.
        //[RemoteAsync]
        //public static IAsyncOperation<IReadOnlyList<GpioController>> GetControllersAsync(IGpioProvider provider);
        //
        // Summary:
        //     Gets the default general-purpose I/O (GPIO) controller for the system.
        //
        // Returns:
        //     The default GPIO controller for the system, or null if the system has no GPIO
        //     controller.
        //[RemoteAsync]
        //public static IAsyncOperation<GpioController> GetDefaultAsync();
        //
        // Summary:
        //     Gets the default general-purpose I/O (GPIO) controller for the system.
        //
        // Returns:
        //     The default GPIO controller for the system, or null if the system has no GPIO
        //     controller.
        //public static GpioController GetDefault()
        //{
        //    return GpioController.GetDefault();
        //}

        //
        // Summary:
        //     Gets the number of pins on the general-purpose I/O (GPIO) controller.
        //
        // Returns:
        //     The number of pins on the GPIO controller. Some pins may not be available in
        //     user mode. For information about how the pin numbers correspond to physical pins,
        //     see the documentation for your circuit board.
        //public int PinCount { get; }
        #endregion
    }
}
