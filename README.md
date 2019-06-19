# Immersal AR Cloud SDK Samples
Example projects that use [Immersal AR Cloud SDK](https://immersal.com/developers/ "Register and download SDK") and demonstrate some of its functionalities. Currently included examples are:

* MultimapSampleScene: a simple scene that localizes the device using previously generated (embedded) maps and displays 3D objects relative to the maps. You need to capture and download your own maps to demonstrate this functionality, see MappingApp below.
* ContentPlacementSample: allows for dropping objects in the AR space. The locations are saved locally, but not persisted across devices.
* NavigationSample: AR wayfinding example
* MappingApp: a full-featured app for mapping spaces using an iPhone or Android phone.

See the [Developer Documentation](https://immersal.com/developers/docs/ "SDK Documentation") to understand how to use these examples.

Compatible with Unity 2018.4 LTS and AR Foundation 1.5. Unity 2019.1 has a bug with large UnityWebRequests on iOS, so it is not officially supported by us yet.
