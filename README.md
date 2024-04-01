# BSDiscordRanking

## Notes

This is the project that made me learn C# and drive upward my codding skills.
For a first programming project, i think it gets the job done, and i think it's always okay to start somewhere.

Since no bugs had been found in this very old, unmaintanable, beginner, nested hell and imperative giant piece of code, i can finally move on and archive this in hope to never work on it again.

## Special Thanks

Special thanks to BSCC for allowing me to test this bot. Then thanks to Saitei for allowing me to implement category leveling and therefore creating the new Challenge Saber sever.
And finally thanks Jupilian for the initial help and for being a good friend.

Also, as a final sidenote, thanks to NustyFrozen.
The ogs only might remember why.

## Customisation

The config file is in the same folder as the binaries and is named "config.json".
It is created on the first start and can be reset with the command "reset-config".

| Input | Type |
| --- | --- |
| Discord Token | string |
| Command prefix | List<string> |
| Discord Status | string |
| BotManagement RoleID | ulong |
| BigGGP (embed) | bool |
| RolePrefix ( prefix for level roles ) | string |
| AuthorizedChannel | List<ulong> |
| GiveOldRoles | bool |

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
| profile | Shows your user profile. |
| ping | Shows bot's latency for Discord & Scoresaber servers. |

### BotManager Commands
| Command | Description |
| --- | --- |
| addmap \<level> \<key> \<Standard/Lawless..> \<ExperPlus/Hard..> | Add a map to a level. |
| removemap \<level> \<key> \<Standard/Lawless..> \<ExperPlus/Hard..> | Remove a map from a level. |
| reset-config | Reset the config file of the bot. The bot stops after running the command. |
| createroles | Create a role for each level. |
| addchannel | Add the channel to autorized channels list |
| removechannel | Remove the channel from autorized channels list |

## Credit

- [Kuurama](https://github.com/Kuurama)
- [Julien "Jupilian" ROPERS](https://github.com/ASPJulien)

## Licence & Dependencys

The MIT License (MIT). Please see [Licence File](https://github.com/Kuurama/BSDiscordRanking/blob/master/LICENSE.md) for more information.
