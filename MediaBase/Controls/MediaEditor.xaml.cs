using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;

using DirectN;

using Microsoft.UI;
using Microsoft.UI.Windowing;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;

using Windows.Devices.Enumeration;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics;

using WinRT;

namespace MediaBase.Controls
{
    public sealed partial class MediaEditor : UserControl
    {
        #region Fields
        private IComObject<IDXGISwapChain1> _swapChain;
        private IComObject<IDXGISurface> _dxgiSurface;
        private IComObject<ID2D1Factory1> _factory;
        private IComObject<ID2D1Device> _device;
        private IComObject<ID2D1DeviceContext> _deviceContext;
        private IComObject<ID2D1Bitmap1> _d2dBitmap;
        #endregion

        #region Properties
        public double DisplayRefreshRate
        {
            get => (double)GetValue(DisplayRefreshRateProperty);
            private set => SetValue(DisplayRefreshRateProperty, value);
        }

        public static readonly DependencyProperty DisplayRefreshRateProperty =
            DependencyProperty.Register("DisplayRefreshRate",
                                        typeof(double),
                                        typeof(MediaEditor),
                                        new PropertyMetadata(60.0,
                                            OnDisplayRefreshRateChanged));
        #endregion

        #region Constructor
        public MediaEditor()
        {
            InitializeComponent();
        }
        #endregion

        #region Dependency Property Callbacks
        private static void OnDisplayRefreshRateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            
        }
        #endregion

        #region Event Handlers (UserControl)
        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            App.Window.Closed += (s, e) => Dispose();
            InitializeDirect2D();
        }

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            Dispose();
        }

        private void SwapChainPanel_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (!IsLoaded)
                return;

            _dxgiSurface.FinalDispose();
            _d2dBitmap.FinalDispose();
            _deviceContext.FinalDispose();

            var hr = _swapChain.Object.ResizeBuffers(2, (uint)e.NewSize.Width, (uint)e.NewSize.Height, DXGI_FORMAT.DXGI_FORMAT_B8G8R8A8_UNORM, 0);
            if (hr == HRESULTS.S_OK)
                CreateRenderTarget();
        }
        #endregion

        #region Event Handlers (Commands)
        #region CanExecuteRequested

        #endregion

        #region ExecuteRequested

        #endregion
        #endregion

        #region Event Handlers (Media Player)

        #endregion

        #region Event Handlers (Timeline)

        #endregion

        #region Event Handlers (Pointer)
        private void RenderAreaBorder_PointerEntered(object sender, PointerRoutedEventArgs e)
        {

        }

        private void RenderAreaBorder_PointerExited(object sender, PointerRoutedEventArgs e)
        {

        }

        private void RenderAreaBorder_PointerPressed(object sender, PointerRoutedEventArgs e)
        {

        }

        private void RenderAreaBorder_PointerReleased(object sender, PointerRoutedEventArgs e)
        {

        }

        private void RenderAreaBorder_PointerCaptureLost(object sender, PointerRoutedEventArgs e)
        {

        }

        private void RenderAreaBorder_PointerCanceled(object sender, PointerRoutedEventArgs e)
        {

        }

        private void RenderAreaBorder_PointerMoved(object sender, PointerRoutedEventArgs e)
        {

        }

        private void RenderAreaBorder_PointerWheelChanged(object sender, PointerRoutedEventArgs e)
        {

        }
        #endregion

        #region Private Methods
        private void Render()
        {
            _deviceContext.BeginDraw();

            _deviceContext.Clear(_D3DCOLORVALUE.Tomato);
            using var dWriteFactory = DWriteFunctions.DWriteCreateFactory(DWRITE_FACTORY_TYPE.DWRITE_FACTORY_TYPE_SHARED);
            using var textFormat = dWriteFactory.CreateTextFormat("Arial", 100.0f);
            using var textBrush = _deviceContext.CreateSolidColorBrush(_D3DCOLORVALUE.White);
            var time = DateTime.Now;
            _deviceContext.DrawText($"{time.Hour}:{time.Minute:00}:{time.Second:00}.{time.Millisecond:000}",
                textFormat, new D2D_RECT_F(25.0, 25.0, 1000.0, 500.0), textBrush);

            _deviceContext.EndDraw();
            _swapChain.Present(1, 0);
        }

        private void InitializeDirect2D()
        {
            var flags = D3D11_CREATE_DEVICE_FLAG.D3D11_CREATE_DEVICE_BGRA_SUPPORT;
#if DEBUG
            flags |= D3D11_CREATE_DEVICE_FLAG.D3D11_CREATE_DEVICE_DEBUG;
#endif

            // Initialize DXGI
            using var d3D11Device = D3D11Functions.D3D11CreateDevice(null,
                                                                     D3D_DRIVER_TYPE.D3D_DRIVER_TYPE_HARDWARE,
                                                                     flags,
                                                                     out _);

            using var dxgiDevice = ComObject.From(d3D11Device.As<IDXGIDevice1>(true));
            using var dxgiAdapter = dxgiDevice.GetAdapter();
            using var dxgiFactory = dxgiAdapter.GetFactory2();

            // Get refresh rate
            var modeToMatch = default(DXGI_MODE_DESC);
            foreach (var output in dxgiAdapter.EnumOutputs())
            {
                var modeDesc = output.FindClosestMatchingMode(modeToMatch, d3D11Device);
                if (modeDesc != null)
                    DisplayRefreshRate = (double)modeDesc.Value.RefreshRate.Numerator / modeDesc.Value.RefreshRate.Denominator;
                output.FinalDispose();
            }

            // Initialize swap chain
            var swapChainDesc = new DXGI_SWAP_CHAIN_DESC1
            {
                Format = DXGI_FORMAT.DXGI_FORMAT_B8G8R8A8_UNORM,
                Scaling = DXGI_SCALING.DXGI_SCALING_STRETCH,
                AlphaMode = DXGI_ALPHA_MODE.DXGI_ALPHA_MODE_UNSPECIFIED,
                SwapEffect = DXGI_SWAP_EFFECT.DXGI_SWAP_EFFECT_FLIP_SEQUENTIAL,
                SampleDesc = new DXGI_SAMPLE_DESC { Count = 1 },
                BufferUsage = Constants.DXGI_USAGE_RENDER_TARGET_OUTPUT,
                BufferCount = 2,
                Width = (uint)panel.ActualWidth,
                Height = (uint)panel.ActualHeight
            };

            _swapChain = dxgiFactory.CreateSwapChainForComposition(dxgiDevice, swapChainDesc);

            // Initialize Direct2D
            _factory = D2D1Functions.D2D1CreateFactory1(D2D1_FACTORY_TYPE.D2D1_FACTORY_TYPE_SINGLE_THREADED);
            _device = _factory.CreateDevice(dxgiDevice);

            // Connect D2D device context to swap chain
            CreateRenderTarget();

            // Set DPI
            _factory.Object.GetDesktopDpi(out var dpiX, out var dpiY);
            _deviceContext.Object.SetDpi(dpiX, dpiY);

            // Connect swap chain to native swap chain panel
            var nativePanel = panel.As<ISwapChainPanelNative>();
            nativePanel.SetSwapChain(_swapChain.Object).ThrowOnError();

            CompositionTarget.Rendering += (s, e) => Render();
        }

        private void CreateRenderTarget()
        {
            var bitmapProperties = new D2D1_BITMAP_PROPERTIES1
            {
                bitmapOptions = D2D1_BITMAP_OPTIONS.D2D1_BITMAP_OPTIONS_TARGET |
                                D2D1_BITMAP_OPTIONS.D2D1_BITMAP_OPTIONS_CANNOT_DRAW,
                pixelFormat = new D2D1_PIXEL_FORMAT
                {
                    format = DXGI_FORMAT.DXGI_FORMAT_B8G8R8A8_UNORM,
                    alphaMode = D2D1_ALPHA_MODE.D2D1_ALPHA_MODE_IGNORE
                }
            };

            _deviceContext = _device.CreateDeviceContext(D2D1_DEVICE_CONTEXT_OPTIONS.D2D1_DEVICE_CONTEXT_OPTIONS_NONE);
            _dxgiSurface = _swapChain.GetBuffer<IDXGISurface>(0);
            _d2dBitmap = _deviceContext.CreateBitmapFromDxgiSurface(_dxgiSurface, bitmapProperties);

            _deviceContext.SetTarget(_d2dBitmap);
        }

        private void Dispose()
        {
            _dxgiSurface.FinalDispose();
            _d2dBitmap.FinalDispose();
            _deviceContext.FinalDispose();
            _swapChain.FinalDispose();
            _factory.FinalDispose();
            _device.FinalDispose();
        }
        #endregion

        #region Native Interop
        [ComImport, Guid("63aad0b8-7c24-40ff-85a8-640d944cc325"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        public partial interface ISwapChainPanelNative
        {
            [PreserveSig]
            HRESULT SetSwapChain(IDXGISwapChain swapChain);
        }
        #endregion
    }
}