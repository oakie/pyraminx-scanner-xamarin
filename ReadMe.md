# LEGO Pyraminx Robot UI App

This is the source code of the Android app seen in this [video](https://youtu.be/F1rV5Vs5Lt0).

While in theory this app works without the physical LEGO MINDSTORMS robot, it has little to no use.

## Getting Started

These instructions will get you a copy of the project up and running on your local machine for development and testing purposes.

### Prerequisites

This project is built on Xamarin Android and therefore requires Visual Studio with the Xamarin SDK.

### Installing

1. Follow the instructions to clone and build the [Xamarin.Android.OpenCV repo](https://github.com/jeremy-ellis-tech/Xamarin.Android.OpenCV)
2. Clone this repo
3. Open Pyraminx.sln and add a project reference to OpenCV.Binding
4. Select the "None" build platform
5. Hit F5 to Build and Deploy to your selected Android device

## Tips

### Reducing build time

Follow these [instructions](https://github.com/jeremy-ellis-tech/Xamarin.Android.OpenCV#reducing-the-dll-size) on reducing the .dll size of the build in order to reduce build times. The third option (Using the None configuration) is recommended, but requires the Andriod device to have the native OpenCV libs installed separately.

## License

This project is licensed under the MIT License - see the [LICENSE.md](LICENSE.md) file for details

## See also

The source code for the LEGO robot can be found [here](https://github.com/oakie/pyraminx-robot-py).
