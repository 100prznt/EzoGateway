![EzoGateway PCB equipped with RaspberryPi and EZO™ devices (pH, ORP and RTD)](img/assembly_sn001_top2.jpg "EzoGateway PCB equipped with RaspberryPi and EZO™ devices (pH, ORP and RTD)")
# EzoGateway Hardware

For a better usability of the EzoGateway a custom PCB was designed, which replaces the Whitebox T3 HAT and adds some features. Designed as an inverted HAT, the PCB carries up to 3 Atlas Scientific EZO™ circuits in addition to the Raspberry Pi.

## Features

| Count | Feature                   | Connector                           | Description                 |
|:-----:|---------------------------|-------------------------------------|-----------------------------|
|       | wide range power supply   | Wago MCS or coaxial power connector | 9 - 36 VDC (min. 5 W)       |
| 2     | isolated EZO sockets      | BNC                                 | for pH and O.R.P. circuit   |
| 1     | non-isolated EZO sockets  | BNC                                 | for RTD circuit             |
| 2/4   | external 1-wire channels  | Wago MCS                            | for digital temp. sensors   |
| 1     | on-board temp. sensor     |                                     | on seperate int. 1-wire ch. |
|       | expansion port (I2C, SPI) | .10" pin header                     |                             |
| 4     | status LEDs               |                                     |                             |
|       | fits for ALUBOS 1000      |                                     |                             |

## PCB design
Top and bottom view of the first PCB manufactured. In production since 20.02.2020.

![PCB of EzoGateway with 1-wire bridge (v01.1)](img/rpi_opc_v01.1_top_pink.png "PCB of EzoGateway with 1-wire bridge (v01.1)")

![PCB of EzoGateway with 1-wire bridge (v01.1)](img/rpi_opc_v01.1_bot_pink.png "PCB of EzoGateway with 1-wire bridge (v01.1)")

## Assembly
First built up "EzoGateway" from 12.03.2020. Equipped with RaspberryPi and EZO™ devices for pH, ORP and RTD) measurement.

![EzoGateway PCB equipped with RaspberryPi and EZO™ devices (pH, ORP and RTD)](img/assembly_sn001_top.jpg "EzoGateway PCB equipped with RaspberryPi and EZO™ devices (pH, ORP and RTD)")

  
## License
[![Creative Commons License](https://i.creativecommons.org/l/by-nc-nd/4.0/88x31.png "Creative Commons License")](http://creativecommons.org/licenses/by-nc-nd/4.0/)

The EzoGateway PCB is licensed under a [Creative Commons Attribution-NonCommercial-NoDerivatives 4.0 International License](http://creativecommons.org/licenses/by-nc-nd/4.0/).