# BSDiscordRanking

## Customisation

The config file is in the same folder as the binaries and is named "config.json".
It is created on the first start and can be reset with the command "reset-config".

| Input | Type |
| --- | --- |
| Discord Token | string |
| Command prefix | string |
| Discord Status | string |
| BotManagement RoleID | ulong |

## Commands

### User Commands
| Command | Description |
| --- | --- |
| help | Send a embed that shows all commands. |
| link \<id> | Link the DiscordID of the user to a ScoreSaberID. |
| unlink | Unlink the ScoreSaberID of the user (does not delete cached files). |
| ggp \<level> | Shows the maps of a level and bar them if passed. |
| gpl \<level or "all"> | Send the playlist file/folder. |
| scan | Scan and store all your scores, check if you passed maps from levels. |

### Admin Commands
| Command | Description |
| --- | --- |
| addmap \<level> \<key> \<Standard/Lawless..> \<ExperPlus/Hard..> | Add a map to a level. |
| reset-config | (ServerOwner only) Reset the config file of the bot. The bot stops after running the command. |

## Credit

- [Kuurama](https://github.com/Kuurama)
- [Julien "Jupilian" ROPERS](https://github.com/ASPJulien)

## Licence & Dependencys

The MIT License (MIT). Please see [Licence File](https://github.com/Kuurama/BSDiscordRanking/blob/master/LICENSE.md) for more information.
