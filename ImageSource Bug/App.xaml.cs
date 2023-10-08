using System.Diagnostics;
using System.Reflection;
using SkiaSharp;

namespace ImageSource_Bug {
    public partial class App : Application {

        Image image1;
        Image image2;
        public event Action screenSizeChangedEvent = null;
        public double screenHeight = 0;
        public double screenWidth = 0;
        ImageLoader loader1;
        ImageLoader loader2;
        List<string> imageList = new();

        //CODE USAGE INSTRUCTIONS:
        //CLICK SCREEN TO LOAD NEXT TWO PHOTOS, KEEP CLICKING UNTIL ERRORS SHOW UP (ANDROID BUG ONLY, WORKS IN IOS/WINDOWS)
        //TYPICALLY WILL HAPPEN WITHIN 1-5 CLICKS

        public App() {
            InitializeComponent();

            ContentPage mainPage = new();
            MainPage = mainPage;

            //screen size monitor
            mainPage.SizeChanged += delegate {
                if (mainPage.Height > 0 && mainPage.Width > 0) {
                    screenWidth = mainPage.Width;
                    screenHeight = mainPage.Height;
                    screenSizeChangedEvent?.Invoke();
                }
            };

            //layout composition
            AbsoluteLayout abs = new();
            mainPage.Content = abs;
                        
            //images
            image1 = new();
            image2 = new();
            image1.Aspect = Aspect.Fill; 
            image2.Aspect = Aspect.Fill;
            abs.Children.Add(image1);
            abs.Children.Add(image2);

            //image loader
            loader1 = new();
            loader2 = new();
            image1.Behaviors.Add(loader1);
            image2.Behaviors.Add(loader2);

            //image reference
            buildImageList();

            //border to take taps
            Border border = new();
            border.BackgroundColor = Colors.White;
            border.Opacity = 0;
            abs.Children.Add(border);

            //tap function
            TapGestureRecognizer tapRecognizer = new();
            border.GestureRecognizers.Add(tapRecognizer);
            tapRecognizer.Tapped += delegate {
                Debug.WriteLine("TAP RECOGNIZED");
                if (imageList.Count > 0) {
                    loader1.loadImageFromEmbeddedResource(imageList[new Random().Next(0, imageList.Count)]);
                    loader2.loadImageFromEmbeddedResource(imageList[new Random().Next(0, imageList.Count)]);

                    resizeAndPositionImages();
                }
            };

            //screen size monitoring
            screenSizeChangedEvent += resizeAndPositionImages;
            screenSizeChangedEvent += delegate {
                border.HeightRequest = screenHeight;
                border.WidthRequest = screenWidth;
            };

            //DebugTools.debugResources();

            Debug.WriteLine("APP BUILT ON MAIN THREAD: " + MainThread.IsMainThread);
        }
        public void buildImageList() {
            for (int i=0; i <11; i++) {
                imageList.Add("ImageSource_Bug.Resources.Images.cats" + (i+1).ToString() + ".jpg");
            }
        }
        public void resizeAndPositionImages() {
            if (loader1.rawWidth > 0 && loader2.rawWidth > 0) {
                loader1.setDimensionsProportionatelyFromHeight(screenHeight * 0.5);
                loader2.setDimensionsProportionatelyFromHeight(screenHeight * 0.5);
                image2.TranslationY = screenHeight * 0.5;
                image1.TranslationX = (screenWidth - loader1.lastWidthRequest) * 0.5;
                image2.TranslationX = (screenWidth - loader2.lastWidthRequest) * 0.5;
            }
        }
    }

    public class ImageLoader : Behavior<Image> {
        public Image imageView;

        //store image raw width/height on accessing
        public int rawWidth = 0;
        public int rawHeight = 0;
        
        string resourceName = null;

        public double lastHeightRequest = 0; 
        public double lastWidthRequest = 0; 

        protected override void OnAttachedTo(Image ve) {
            imageView = ve;
            if (rawHeight > 0 && resourceName != null) {
                applyPhoto();
            }
            base.OnAttachedTo(ve);
        }
        protected override void OnDetachingFrom(Image ve) {
            imageView = null;
            base.OnDetachingFrom(ve);
        }
        public void loadImageFromEmbeddedResource(string resourceName, bool justReadForSizeManagement = false) { //make it just for size management if nothing to apply - applying photo throgh tinter instead

            Debug.WriteLine("LOAD IMAGE FROM EMBEDDED RESOURCE IS ON MAIN THREAD: " + MainThread.IsMainThread);

            this.resourceName = resourceName;
            Assembly assembly = GetType().GetTypeInfo().Assembly;

            //https://stackoverflow.com/questions/10984336/using-statement-vs-idisposable-dispose
            using (Stream stream = assembly.GetManifestResourceStream(resourceName)) {
                SKBitmap rawBitmap = SKBitmap.Decode(stream); // a bit inefficient as must decode fully bitmap just to get height/width but good enough //DISCARDS THE STREAM
                rawHeight = rawBitmap.Height;
                rawWidth = rawBitmap.Width;

                rawBitmap.Dispose();
                Debug.WriteLine("GOT IMAGE WIDTH " + rawWidth + " HEIGHT " + rawHeight + " " + resourceName + " ON MAIN THREAD: " + MainThread.IsMainThread);

            }
            if (!justReadForSizeManagement) {
                applyPhoto();
            }

        }
        private void applyPhoto() {

            Debug.WriteLine("APPLY PHOTO IS ON MAIN THREAD: " + MainThread.IsMainThread);

            MainThread.BeginInvokeOnMainThread(new Action(() => {
                if (resourceName != null && imageView != null) {

                    Assembly assembly = GetType().GetTypeInfo().Assembly;
                    Stream getFromStream() { return assembly.GetManifestResourceStream(resourceName); };

                    Debug.WriteLine("IMAGE SOURCE FROM RESOURCE IS ON MAIN THREAD: " + MainThread.IsMainThread);
                    //imageView.Source = ImageSource.FromStream(getFromStream);
                    imageView.Source = ImageSource.FromResource(resourceName, assembly); 
                    Debug.WriteLine("IMAGE SOURCE FINISHED ON MAIN THREAD: " + MainThread.IsMainThread);
                }
            }));

        }
        public void setDimensionsProportionatelyFromHeight(double heightToSet) { //return the expected height for reference
            if (rawHeight > 0 && rawWidth > 0) {

                lastHeightRequest = heightToSet;
                lastWidthRequest = heightToSet * rawWidth / rawHeight;

                imageView.WidthRequest = lastWidthRequest;
                imageView.HeightRequest = lastHeightRequest;

                Debug.WriteLine("RAW WIDTH " + rawWidth + " " + rawHeight + " height request " + heightToSet + " Current width " + imageView.Width + " IS ON MAIN THREAD: " + MainThread.IsMainThread);
            }
        }
    }
    public static class DebugTools {
        public static void debugResources() {
            foreach (string currentResource in Assembly.GetExecutingAssembly().GetManifestResourceNames()) {
                Debug.WriteLine(currentResource);
            }
        }
    }

}