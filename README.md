# CRI-DemoClient
[![Build](https://github.com/CommonplaceRobotics/CRI-DemoClient/actions/workflows/build.yml/badge.svg)](https://github.com/CommonplaceRobotics/CRI-DemoClient/actions/workflows/build.yml)

C# demo client to connect with the igus Robot Control using the CRI ethernet interface. 

This client is intended as an example on how to send the most common CRI messages, it should not be used as a base for production applications. The CRI protocol documentation can be found in (this guide)[https://wiki.cpr-robots.com/index.php/CRI_Ethernet_Interface]. (This article)[https://wiki.cpr-robots.com/index.php/CRI_Client_Structure] explains how to set up a basic CRI client.

The client can also be used to test CRI messages: Using the text box in the "Custom" tab on the right side you can send your own CRI messages and observe the response on the log box below.

If you do not need to send configuration messages or jog the robot consider using the (App Interface)[https://wiki.cpr-robots.com/index.php/Apps_for_the_Robot_Control] instead.
