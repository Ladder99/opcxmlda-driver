# opcxmlda-driver

## Overview

Fully configurable OPC XML-DA driver built on top of [base-driver](https://github.com/Ladder99/base-driver) and [QuickOPC libraries](https://www.opclabs.com/products/picoopc/227-products/quickopc).  Perfect for pulling data from Simotion drives running on Simatic Step 7 before OPC-UA was added.

Output:
* transport: mqtt, format: Splunk metric.
* transport: mqtt and tcp, format: SHDR (https://github.com/mtconnect/dot_net_sdk).

TODO: documentation