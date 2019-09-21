## Project under construction :construction:
```diff
! This project is currently (2019/09/20) under construction, in a very early phase...
```

---

![EZO Devices on the Whitebox carrier](docu/img/wb-ezo-hat.jpg "Atlas Scientific EZO™ devices (pH, ORP and RTD) on the Whitebox carrier.")
# EzoGateway
Open source UWP App, to brings the Atlas Scientific EZO™ devices in the __Internet of Things__. Per __REST API__ you can fetch live measdata and calibrate connected sensors.
Ideal for monitoring water quality in your pool.


[![Bulid](https://img.shields.io/appveyor/ci/100prznt/ezogateway.svg?logo=appveyor&style=popout-square)](https://ci.appveyor.com/project/100prznt/ezogateway)   [![GitHub issues](https://img.shields.io/github/issues/100prznt/EzoGateway?logo=github&style=popout-square)](https://github.com/100prznt/EzoGateway/issues)   [![Code size](https://img.shields.io/github/languages/code-size/100prznt/EzoGateway.svg?logo=github&style=popout-square)](#) 

## How to install?
TODO!

## How to use?

### API Reference
Moved in the Wiki -> [API Reference](https://github.com/100prznt/EzoGateway/wiki/API-Reference)

### Web interface
![EzoGateway - live data](docu/img/web-interface-live-data.png "EzoGateway web interface shows live measurement data.")

## Hardware
For fast hardware integration there is a cool project from [Whitebox](https://github.com/whitebox-labs). The [Tentacle T3 HAT](https://github.com/whitebox-labs/tentacle-raspi-oshw) accepts three Atlas Scientific EZO™ devices, two of them are electrically isolated.

## Releases
This project build on the continuous integration (CI) platform [AppVeyor](https://www.appveyor.com/) and released in the [Release-Feed](https://github.com/100prznt/EzoGateway/releases).

[![AppVeyor Bulid](https://img.shields.io/appveyor/ci/100prznt/ezogateway.svg?logo=appveyor&style=popout-square)](https://ci.appveyor.com/project/100prznt/ezogateway)  
[![AppVeyor Tests](https://img.shields.io/appveyor/tests/100prznt/EzoGateway/master.svg?logo=appveyor&style=popout-square)](https://ci.appveyor.com/project/100prznt/EzoGateway/build/tests)

[![GitHub Release](https://img.shields.io/github/release/100prznt/EzoGateway.svg?logo=github&style=popout-square)](https://github.com/100prznt/EzoGateway/releases/latest)  
[![GitHub (Pre-)Release](https://img.shields.io/github/release/100prznt/EzoGateway/all.svg?logo=github&style=popout-square)](https://github.com/100prznt/EzoGateway/releases) (Pre-)Release

## Credits
This app is made possible by contributions from:
* [Elias Rümmler](http://www.100prznt.de) ([@rmmlr](https://github.com/rmmlr)) - core contributor

### Open Source Project Credits

* [Rca.EzoDeviceLib](https://github.com/100prznt/Rca.EzoDeviceLib) - UWP driver library for the Atlas Scientific EZO™ devices
* [Newtonsoft.Json](https://www.newtonsoft.com/json) Object serialization for REST API and local storage
* [UIkit](https://github.com/uikit/uikit) Style up the web interface
* [jQuery](https://github.com/jquery/jquery) Web interface data exchange

## License
The EzoGateway App is licensed under [MIT](http://www.opensource.org/licenses/mit-license.php "Read more about the MIT license form"). Refer to [LICENSE.txt](https://github.com/100prznt/EzoGateway/blob/master/LICENSE.txt) for more information.

## Contributions
Contributions are welcome. Fork this repository and send a pull request if you have something useful to add.
