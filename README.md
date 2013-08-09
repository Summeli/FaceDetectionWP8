FaceDetectionWP8: 
===========================

FaceDetection library for Windows Phone 8. It's a port of [facedetectwp7] library by Julia Schwarz.
This library uses the same algorithms and detection models as OpenCV and is written in C# and built for the Windows Phone.

Note: this library can actually detect objects of any type, provided you give it the correct model files, since all it does is read existing, learned models. You can find model files on [opencv] website.


How to Use the Library
===========================
This library is built for Windows Phone 8 and is intended to be used only for Windows Phone 8.

To use the library:
Download or fork the project from Gitub.
Either add the FaceDetectionWinPhone Project to your project or build/add the FaceDetectionWinPhone dll
Add a models/ folder to your project and add xml model files as found under FaceDetectionWinPhone/Sample/models or in the downloads section. I would recommend using haarcascadefrontalfacealt.xml (this is the only one tested).
Make sure you have a reference to System.Xml.Linq
Create a detector: 
```c
FaceDetectionWinPhone.Detector detector = new FaceDetectionWinPhone.Detector(XDocument.Load(MODEL_FILE));
```

You can use the detector by passing in either a string (path to image file), a WriteableBitmap, or an int array, width and height. Here's how to detect faces by passing in an int array:
```c
  List<FaceDetectionWinPhone.Rectangle> faces = mdetector.getFaces(pixelDataInt, mcameraWidth / mdownsampleFactor, mcameraHeight / m_downsampleFactor, 2f, 1.25f, 0.1f, 1, false);
```

Example Application
===========================
This project contains a new example application. You can still get the old wp7 example app from [facedetectwp7] project pages, if you're interested about new examples.

Acknowledgements
===========================
This library was originally made for Windows Phone 7 by Julia Schwarz, and it's only build for wp8 by me.The library is also largely based off of the Java-based library at [jviolajones]. Thank you very much for the developers of this library for making their code open source!

[facedetectwp7]: http://facedetectwp7.codeplex.com/ "facedetectwp7"
[opencv]: http://opencv.org/ "OpenCV"
[jviolajones]: http://code.google.com/p/jviolajones/ 
