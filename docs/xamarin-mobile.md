# Using Indy SDK and Agent Framework with Xamarin

[Working with Xamarin](#working-with-xamarin)

[Instructions for Android](#instructions-for-anroid)

[Instructions for iOS](#instructions-for-ios)

---

## Working with Xamarin

When working with Xamarin, we can fully leverage the offical [Indy wrapper for dotnet](https://github.com/hyperledger/indy-sdk/tree/master/wrappers/dotnet), since the package is fully compatible with Xamarin runtime. The wrapper uses `DllImport` to invoke the native Indy library which exposes all functionality as C callable functions. In order to make the library work in Xamarin, we need to make libindy available for Android and iOS, which requires bundling static libraries of libindy and it's dependencies built for each platform.

## Instructions for Android

To setup Indy on Android you need to add the native libindy references and dependencies. The process is described in detail at the official Xamarin documentation [Using Native Libraries with Xamarin.Android](https://docs.microsoft.com/en-us/xamarin/android/platform/native-libraries).

Below are a few additional things that are not covered by the documentation that are Indy specific.

For Android the entire library and it dependencies are compiled into a single shared object (*.so). In order for `libindy.so` to be executable we must also include `libgnustl_shared.so`.

Depending on the target abi(s) for the resulting app, not all of the artifacts need to be included, for ease of use below we document including all abi(s).

### Setup the Android references

In Visual Studio (for Windows or Mac) create new Xamarin Android project. If you want to use Xamarin Forms, the instructions are the same. Apply the changes to your Android project in Xamarin Forms.

Download the static libraries required [here](../samples/xamarin-mobile-sample/libs-android) or other versions of libindy can be found [here](https://repo.sovrin.org/android/libindy/)

The required files can be added via your IDE by clicking Add-Item and setting the build action to `AndroidNativeLibrary`. However when dealing with multiple ABI targets it is easier to manually add the references via the android projects .csproj. Note - if the path contains the abi i.e `..\x86\library.so` then the build process automatically infers the target ABI.

If you are adding all the target ABI's to you android project add the following snippet to your .csproj.

```lang=xml
<ItemGroup>
    <AndroidNativeLibrary Include="..\libs-android\armeabi\libindy.so" />
    <AndroidNativeLibrary Include="..\libs-android\arm64-v8a\libindy.so" />
    <AndroidNativeLibrary Include="..\libs-android\armeabi-v7a\libindy.so" />
    <AndroidNativeLibrary Include="..\libs-android\x86\libindy.so" />
    <AndroidNativeLibrary Include="..\libs-android\x86_64\libindy.so" />
    <AndroidNativeLibrary Include="..\libs-android\armeabi\libgnustl_shared.so" />
    <AndroidNativeLibrary Include="..\libs-android\arm64-v8a\libgnustl_shared.so" />
    <AndroidNativeLibrary Include="..\libs-android\armeabi-v7a\libgnustl_shared.so" />
    <AndroidNativeLibrary Include="..\libs-android\x86\libgnustl_shared.so" />
    <AndroidNativeLibrary Include="..\libs-android\x86_64\libgnustl_shared.so" />
  </ItemGroup>
```

Note - paths listed above will vary project to project.

Next we need to invoke these dependencies at runtime. To do this add the following to your MainActivity.cs

```lang=csharp
  JavaSystem.LoadLibrary("gnustl_shared");
  JavaSystem.LoadLibrary("indy");
```

In order to use most of libindy's functionality, the following permissions must be granted to your app, you can do this by adjusting your AndroidManifest.xml, located under properties in your project.

```lang=xml
  <uses-permission android:name="android.permission.WRITE_EXTERNAL_STORAGE" />
  <uses-permission android:name="android.permission.READ_EXTERNAL_STORAGE" />
  <uses-permission android:name="android.permission.INTERNET" />
```

If you are running your android app at API level 23 and above, these permissions also must be requested at runtime, in order to do this add the following to your MainActivity.cs

```lang=csharp
  if (Build.VERSION.SdkInt >= BuildVersionCodes.M)
  {
      RequestPermissions(new[] { Manifest.Permission.ReadExternalStorage }, 10);
      RequestPermissions(new[] { Manifest.Permission.WriteExternalStorage }, 10);
      RequestPermissions(new[] { Manifest.Permission.Internet }, 10);
  }
```

Next, install the Nuget packages for Indy SDK and/or Agent Framework and build your solution. Everything should work and run just fine.
If you run into any errors or need help setting up, please open an issue in this repo.

Finally, check the Xamarin Sample we have included for a fully configured project.

## Instructions for iOS

To setup Indy on iOS you need to add the native libindy references and dependencies. The process is described in detail at the official Xamarin documentation [Native References in iOS, Mac, and Bindings Projects](https://docs.microsoft.com/en-us/xamarin/cross-platform/macios/native-references).

Below are a few additional things that are not covered by the documentation that are Indy specific.

In order to enable the Indy SDK package to recognize the `DllImport` calls to the native static libraries, we need to include them in our solution.


These includes the following static libraries:

- libindy.a
- libssl.a
- libsodium.a
- libcrypto.a
- libzmq.a

The Indy team doesn't provide static libraries for all of the dependencies, so we need to build them ourselves. We have included pre-built libraries in our samples project, you can use these to get started quickly, or you can build them yourself. Here are some helpful instructions on building the dependencies for iOS should you decide to take that route.

[Open SSL for iOS](https://github.com/x2on/OpenSSL-for-iPhone)

[Build ZeroMQ library](https://www.ics.com/blog/lets-build-zeromq-library)

[libsodium script of iOS](https://github.com/jedisct1/libsodium/blob/master/dist-build/ios.sh)

The above links should help you build the 4 static libraries that libindy depends on. To build libindy for iOS, check out the offical Indy SDK repo or [download the library from the Sovrin repo](https://repo.sovrin.org/ios/libindy/).

### Setup the iOS references

In Visual Studio (for Windows or Mac) create new Xamarin iOS project. If you want to use Xamarin Forms, the instructions are the same. Apply the changes to your iOS project in Xamarin Forms.

Download the static libraries required [here](../samples/xamarin-mobile-sample/libs-ios) or other versions of the static libraries can be found [here](https://repo.sovrin.org/ios/libindy/)
Add each library as native reference, either by right cicking the project and Add Native Reference, or add them directly in the project file.

Make sure libraries are set to `Static` in the properties window, and additionally `Is C++` is checked for `libzqm.a` only.

The final project file should look include this (paths will vary per project):

```lang=xml
  <ItemGroup>
    <NativeReference Include="..\libs-ios\libcrypto.a">
      <Kind>Static</Kind>
    </NativeReference>
    <NativeReference Include="..\libs-ios\libsodium.a">
      <Kind>Static</Kind>
    </NativeReference>
    <NativeReference Include="..\libs-ios\libssl.a">
      <Kind>Static</Kind>
    </NativeReference>
    <NativeReference Include="..\libs-ios\libzmq.a">
      <Kind>Static</Kind>
      <IsCxx>True</IsCxx>
    </NativeReference>
    <NativeReference Include="..\libs-ios\libindy.a">
      <Kind>Static</Kind>
    </NativeReference>
  </ItemGroup>
```

Next, in your project options under `iOS Build` add the following additional MTouch arguments

`-gcc_flags -dead_strip -v`

This step is cruical, otherwise you won't be able to build the project. It prevents linking unused symbols in the static libraries. Make sure you add these arguments for all configurations, not just Debug.
If you prefer to add them directly in the project file, add the following line:

```lang=xml
<MtouchExtraArgs>-gcc_flags -dead_strip -v</MtouchExtraArgs>
```

Next, install the Nuget packages for Indy SDK and/or Agent Framework and build your solution. Everything should work and run just fine.
If you run into any errors or need help setting up, please open an issue in this repo.

Finally, check the Xamarin Sample we have included for a fully configured project.