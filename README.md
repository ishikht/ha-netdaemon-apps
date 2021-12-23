# Home Assistant NetDaemon Apps

- Tuya battery powered Window/Door Wifi sensors integration
- Terneo heat floor WiFi thermostats
- Midea AC integration (Doesn't support Fan speed and Swing modes at the moment)

#Installation
- Install NetDaemon add-on. Refer documentation https://netdaemon.xyz/docs/started/installation. <br /> 
For Tuya and Terneo integrations use my fork as the Repository URL https://github.com/ishikht/homeassistant-addon. 
These integrations requires network permissions to be set, otherwise integrations might not work properly.
- Install NetDaemon integration, refer documentation https://netdaemon.xyz/docs/started/integration
- Copy apps folder from the repository /NetDaemonApps/app to NetDaemon app folder in Home Assistant.
To install some specific integration copy:
  - /NetDaemonApps/app/libs/
  - /NetDaemonApps/app/[integration]/
  - /NetDaemonApps/app/global.cs
- Run NetDaemon add-on 