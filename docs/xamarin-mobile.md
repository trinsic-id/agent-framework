# Using Indy SDK and Agent Framework with Xamarin

Contents

[Working with Xamarin](#working-with-xamarin)

[Instructions for Android](#instructions-for-anroid)

[Instructions for iOS](#instructions-for-ios)

---

## Working with Xamarin

TODO

## Instructions for Android

TODO

## Instructions for iOS

To setup Indy on iOS you need to add the native libindy references and dependencies. The process is described in details at the official Xamarin documentation for [Native References in iOS, Mac, and Bindings Projects](https://docs.microsoft.com/en-us/xamarin/cross-platform/macios/native-references).
There are few additional things that are not covered by the documentation that are Indy specific.

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

Download the static libraries required [here](../samples/xamarin-mobile-sample/libs-ios)
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