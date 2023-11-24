![GitHub all releases](https://img.shields.io/github/downloads/ST-Apps/CS2-ExtendedRoadUpgrades/total)
![GitHub release (latest by SemVer including pre-releases)](https://img.shields.io/github/downloads-pre/ST-Apps/CS2-ExtendedRoadUpgrades/latest/total)
﻿[![CC BY-NC-SA 4.0][cc-by-nc-sa-shield]][cc-by-nc-sa]
![Static Badge](https://img.shields.io/badge/PayPal-donate-blue?logo=paypal&link=https%3A%2F%2Fpaypal.me%2FSTApps)


This work is licensed under a
[Creative Commons Attribution-NonCommercial-ShareAlike 4.0 International License][cc-by-nc-sa].

[![CC BY-NC-SA 4.0][cc-by-nc-sa-image]][cc-by-nc-sa]

[cc-by-nc-sa]: http://creativecommons.org/licenses/by-nc-sa/4.0/
[cc-by-nc-sa-image]: https://licensebuttons.net/l/by-nc-sa/4.0/88x31.png
[cc-by-nc-sa-shield]: https://img.shields.io/badge/License-CC%20BY--NC--SA%204.0-lightgrey.svg

# Cities: Skylines 2 - Extended Road Upgrades

> [!IMPORTANT]  
> We're in the early stages of C:S2 modding and the tooling is not ready yet so please take this mod as it is and accept any issue.

This mod enables both **quays**, **retaining walls** and **elevated** upgrade options in the Network Upgrade tool.

> [!WARNING]  
> This mod might mess up your save game. If this happens and some of your roads are acting weird you can bulldoze and recreate them to fix any issue.

[CHANGELOG](./CHANGELOG.md)

# Setup

Each release will contain 4 different download zips:
- `ExtendedRoadUpgrades_<MOD VERSION>-BepInEx5.zip`
- `ExtendedRoadUpgrades_<MOD VERSION>-BepInEx5_Debug.zip`
- `ExtendedRoadUpgrades_<MOD VERSION>-BepInEx6.zip`
- `ExtendedRoadUpgrades_<MOD VERSION>-BepInEx6_Debug.zip`

> [!TIP]
> `Debug` versions can be helpful to have extended logging in case of errors, they're not intended for daily usage

Simply download the release that matches your BepInEx version from the [releases](https://github.com/ST-Apps/CS2-ExtendedRoadUpgrades/releases) page and extract the ZIP file to your `BepInEx/plugins` folder.

> [!TIP]
> If you want to have some debug output, please download the `_Debug.zip` version from the releases page.

## Requirements

- [Cities: Skylines 2](https://store.steampowered.com/app/949230/Cities_Skylines_II/)
- BepInEx, either v5 or v6
	- [BepInEx 5.4.22](https://github.com/BepInEx/BepInEx/releases/tag/v5.4.22)
	- [BepInEx-Unity.Mono-win-x64-6.0.0-be.674+82077ec](https://builds.bepinex.dev/projects/bepinex_be)

# Usage

You will find new icons in the Road Upgrade menu, just after the _Grass_ one.

Select one of them and upgrade your roads as you would with the other available upgrades.

# Known Issues

- Retaining walls will generate empty spaces on connecting nodes

# Thanks

Many thanks to the people of [Cities 2 Modding](https://discord.gg/DZaSSnRG) and [Cities: Skylines Modding](https://discord.gg/ey6kT5kf) Discord
communities for their help and pointers that helped me developing this mod.

Special thanks to [Chamëleon TBN](https://github.com/chameleon-tbn) for his beautiful icons and to [Captain of Coit](https://github.com/Captain-Of-Coit) for his mod template.
