# ArcadiaCustoms
The custom arcade mod for Project Arrhythmia that allows users to play levels that aren't on the Steam Workshop.

# What Does it Do?
It does quite a few things, most of it relating to the arcade menu and loading arcade levels. Instead of levels being loaded from Steam, they will be loaded from a custom arcade path (Default: beatmaps/story) that can be changed via the new arcade menu. It also does a few of the things EditorManagement does with adding new difficulties, fixing the instant keyframe and making object color slots go up to 18 rather than 9. Unfortunately at the moment it does not implement pos Z axis as I'm not sure how the game would handle two mods trying to "edit" (Transpile) the code to add the pos Z axis.

Whenever entering the "Specifiy Simulations" screen, the arcade list starts reloading (It reloads a LOT faster than regular Legacy and probably most versions of PA). When you enter the arcade screen, instead of it loading the usual arcade menu, it creates a new UI very much like the editor UI.

https://github.com/RTMecha/ArcadiaCustoms/assets/125487712/c3cd597f-b0f3-4f68-a67a-78b8945f9a76
