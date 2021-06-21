# Gevel-Configurator
Facade Configurator for buildings. Using pointcloud data from Cyclomedia.   
Made in collaboration with "Stichting Happy Balance".

## Contents
- [Files](#files)
- [Installation](#installation)
- [Running the Application](#running-the-application)
- [Usage](#usage)
- [TODO](#todo)
- [Common Errors](#common-errors)
- [Tips](#tips)
- [Used Assets](#used-assets)
- [Collaborators](#collaborators)


## Files 

|File | Content |
|:----|:--------|
| [AssetSpawner.cs](https://github.com/KingPungy/Gevel-Configurator/blob/master/Gevel%20Configurator/Assets/Scripts/AssetSpawner.cs) | Used to load objects from an Import_Folder and display them as panels in the Asset Menu that can be clicked to spawn the object|
| [GizmoManager.cs](https://github.com/KingPungy/Gevel-Configurator/blob/master/Gevel%20Configurator/Assets/Scripts/GizmoManager.cs) | Used to attach Moving/Rotating/Scaling Gizmos to selected objects and for Copy/Pasting/Instantiating objects |
| [SaveLoadManager.cs](https://github.com/BertSlot/Gevel-Configurator/blob/master/Gevel%20Configurator/Assets/Scripts/SaveLoadManager.cs) | Used to save and load scene objects in a JSON format |
| [LoadBinCacheIfAvailable.cs](https://github.com/BertSlot/Gevel-Configurator/blob/master/Gevel%20Configurator/Assets/Plugins/PointCloudTools/Demos/PointCloudViewer/Scripts/LoadBinCacheIfAvailable.cs) | Script to check if cached .bin file of a previously used pointcloud exits and then load using [PointCloudViewerDX11](https://github.com/BertSlot/Gevel-Configurator/blob/master/Gevel%20Configurator/Assets/Plugins/PointCloudTools/PointCloudViewerDX11/Scripts/PointCloudViewerDX11.cs) instead of [RuntimeViewerDX11](https://github.com/BertSlot/Gevel-Configurator/blob/master/Gevel%20Configurator/Assets/Plugins/PointCloudTools/PointCloudViewerDX11/Scripts/RuntimeViewerDX11.cs) if the .bin exists |
| [PointCloudViewerDX11.cs](https://github.com/BertSlot/Gevel-Configurator/blob/master/Gevel%20Configurator/Assets/Plugins/PointCloudTools/PointCloudViewerDX11/Scripts/PointCloudViewerDX11.cs) | Reads the pointcloud data out of a custom binary file generated before in the RuntimeViewerDX11.cs |
| [RuntimeViewerDX11.cs](https://github.com/BertSlot/Gevel-Configurator/blob/master/Gevel%20Configurator/Assets/Plugins/PointCloudTools/PointCloudViewerDX11/Scripts/RuntimeViewerDX11.cs) | Reads and displays pointcloud file at runtime and generates a custom binary for quicker use of the same pointcloud in the future |
| [Object_overzicht.cs](https://github.com/KingPungy/Gevel-Configurator/blob/master/Gevel%20Configurator/Assets/Scripts/Objects_overzicht.cs)| This script calculates the total surface area per surface group and stores this data in a dictionary that is used to export to an excel file or show in the "overzicht panel" |
| [SurfaceOverzicht.cs](https://github.com/KingPungy/Gevel-Configurator/blob/master/Gevel%20Configurator/Assets/Scripts/SurfaceOverzicht.cs) | This script  manages the Surface overzicht panel in the view dropdown. It makes sure the text is properly aligned and that the toggle button above switches between cm²/m² |
| [SurfaceAreaGroup.cs](https://github.com/KingPungy/Gevel-Configurator/blob/master/Gevel%20Configurator/Assets/Scripts/SurfaceAreaGroup.cs) | This script gets attached to each object and stores the names of the surface groups it is a part of, and whether or not it needs to be counted |
| [SceneObjects.cs](https://github.com/KingPungy/Gevel-Configurator/blob/master/Gevel%20Configurator/Assets/Scripts/SceneObjects.cs) | This script manages the list of objects on the side of the screen, and also the properties menu below that |
| [SelectAsset.cs](https://github.com/KingPungy/Gevel-Configurator/blob/master/Gevel%20Configurator/Assets/Scripts/SelectAsset.cs) | This script handles the clicking on the asset panels so that when you double click it spawns the object using a function inside the [GizmoManager.cs](https://github.com/KingPungy/Gevel-Configurator/blob/master/Gevel%20Configurator/Assets/Scripts/GizmoManager.cs) |
| [SelectObject.cs](https://github.com/KingPungy/Gevel-Configurator/blob/master/Gevel%20Configurator/Assets/Scripts/SelectObject.cs) | This script handles the selecting of objects. It makes sure that the object you select also exists in the scene objects list and then selects it in both the scene and the side menu |
| [ViewMode.cs](https://github.com/KingPungy/Gevel-Configurator/blob/master/Gevel%20Configurator/Assets/Scripts/ViewMode.cs) | This script contains functions that cause a switch between working mode and view mode. In view mode all UI is hidden except for the close button |




## Installation
First off you'll need to download [Unity Hub & Unity3D](https://unity3d.com/get-unity/download).  

Once those are installed its time to clone this repository into your desired folder. 
Next you start up Unity Hub and follow any necessary instructions until you arrive at an empty Projects screen.

Click on [ ADD ] to open a project existing on your computer.

<details open>
<summary>Unity Hub projects</summary>
<br> 
 
![UnityHub2.png](RepoInfo/UnityHub2.png)   
 
</details>
 
This will open a 'Select a project to open...' panel in which you will select the highlighted folder inside the cloned repository to open the Unity3D project. 

<details open>
<summary>Project Selection panel</summary>
<br> 
 
![ProjectSelect.png](RepoInfo/ProjectSelect.png)

</details>

## Running the Application

After you've followed the previous steps you'll be ready to open the project.  

The first time you open the project you might be greeted with an empty screen without any scenes loaded. 
To populate the Scene drag a Unity Scene from the Assets/Scenes folder into the Hierarchy tab. The most up to date scene should be main.unity .  

To build and run the application you first need to set the desired build folder. To access the build settings see picture below. 

<details open>
<summary>File/Build Settings</summary>
<br> 
 
![FileBuildSetings.png](RepoInfo/FileBuildSettings.png)

</details>  

This will open the following menu.

<details open>
<summary>Build Settings Menu</summary>
<br> 
 
![BuildSettings.png](RepoInfo/BuildSettings.png)

</details>  

Press the [ Build ] button and a 'Build Windows' panel will open. Select the highlighted folder inside the repository folder. If this folder does not exist create a folder with the name B/build. Folders with this name will be ignored and not uploaded to Github.

<details open>
<summary>Build Windows Panel</summary>
<br> 
 
![BuildFolder.png](RepoInfo/BuildFolder.png)

</details>

After Selecting the build folder the application will begin building and saving into the target folder. The application can now be run by executing the 'Gevel Configurator.exe' inside the build folder. 

###### PS. Make sure there are no errors otherwise the application can not be build. Errors sometimes only arise when trying to build so do this regularly.

## Usage

### Table of contents
- [Keybindings](#keybindings)
- [Camera](#camera)
- [Navigation Bar Buttons](#navigation-bar-buttons)
  * [File Dropdown](#file-dropdown)
  * [Edit Dropdown](#edit-dropdown)
  * [View Dropdown](#view-dropdown)
- [Menus](#menus)
  * [Objects List](#objects-list)
  * [Properties](#properties)
  * [Asset Menu](#asset-menu)

### Keybindings

Gizmo tools types and shortcuts

<details>
<summary>Keybindings</summary>
<br> 
 
|Tool|Keybind|
|:---|:------|
| Move Tool              | W |
| Rotating Tool          | E |
| Scaling Tool           | R |
| Universal tool         | T |
| Transform Space Global | G |
| Transform Space Local  | L |
| Copy                   | Crtl + C |
| Paste                  | Crtl + V |
| Save                   | Ctrl + S |
| Save as new File       | Ctrl + Shift + S |
| Delete                 | Del |
| Alternate Tool Mode    | Hold LShift while using a tool |

</details>

### Camera
<details>
<summary>Camera Movement</summary>
<br> 

|Button|Use|
|:-----|:--|
| WASD               | Move forward/back/sideways                       |
| Hold RMButton      | Rotate camera                                    |
| Hold MiddleMButton | Pan camera verticaly and horizontaly on its axis |
| Scroll Wheel       | Move forwards and backwards , Zoom               |

</details>

### Navigation Bar Buttons

#### File Dropdown
<details>
<summary>File Drowpdown menu</summary>
<br> 
 
|Button|Use|
|:-----|:--|
| New     | W.I.P |
| Load    | Opens an "openfilepanel" to select a save file to open into the scene |
| Save    | Save scene to current opened save file.If there is no current save file acts like "Save As" |
| Save As | Save scene to new save file |
| Import  | Opens an "openfilepanel" to load a pointcloud file into the scene |
| Settings| W.I.P |
| Quit    | Exit Application |

</details>

#### Edit Dropdown
<details>
<summary>Edit Drowpdown menu</summary>
<br> 
 
|Button|Use|
|:-----|:--|
| Redo       | redo's last move/rotate/scale action |
| Undo       | undo's last move/rotate/scale action |
| Copy       | Copy selected objects |
| Cut        | W.I.P |
| Paste      | Paste copied objects |
| Asset Menu | Opens Asset menu |

</details>


#### View Dropdown
<details>
<summary>View Drowpdown menu</summary>
<br> 
 
|Button|Use|
|:-----|:--|
| Overzicht | Opens panel that shows surface Totals and a button for exporting to Excel |
| Demo (F5) | Hides all UI for presentation, does not restrict tool use |

</details>

### Menus

#### Objects List
Shows all objects in the scene in a scrollable viewport on the right side of the screen

#### Properties
Is supposed to show basic information about the object such as:
- Size, Rotation, Position
- Surface Group (Missing multiple selection and custom group names)
- Background color of the object
- W.I.P background Image
- Object Name

#### Asset Menu

Double Clicking on a object preview spawns the object i the scene between the camera and the object you are looking at.

- Search bar (Press search manualy after typing query)
- W.I.P Import Button to import new assets into your project


## TODO

Done:  
✅ Object Background Color changing   
✅ Changing Scale, Rotation, Position  
✅ Attaching Gizmos to objects for transform movements  
✅ Spawning assets from the asset menu  
✅ Object list that shows all objects in the scene   
✅ Object Selection Outline using outline asset from unity asset store   
✅ Correctly Loading .obj files that have multiple meshes  
✅ Loading in PointClouds via import with a windows "openFilePanel"    
✅ Loading Screen for loading pointclouds   
✅ Save/Loading projects in JSON   
✅ Blocking Shortcuts while naming an object to prevent accidental deletion  
✅ Object Naming changed from "Cube(Clone)4gf32(Clone)mf834" to "Cube(1), Cube(2) ... Cube(99)" 


Todo:  
⬜ Error Handling   
⬜ Transform Gizmo Offset bug  
⬜ Importing .obj files at runtime  
⬜ Toolbar current tool feedback/highlight    
⬜ Export to Excel in build application ( Works in unity Editor but not when build )   
⬜ Save the latest resize resolution so that when you go to fullscreen and come back from fullscreen it uses the latest resolution again     
⬜ Multiple Selection Outline fix ( Outlines that overlap block eachother from rendering )      
⬜ Multiple Selection Properties ( Transform capabilities for all selected objects at once )      
⬜ Surface Group Selection ( Selecting multiple groups for an object using [Toggle groups](https://docs.unity3d.com/Packages/com.unity.ugui@1.0/manual/script-ToggleGroup.html)) 
     
⬜ Allow Grouping of objects so that they move as if they were one single object. Also make it posible to tag some of these objects as unscalable to keep proportions      
     
⬜ Start menu for loading projects    
⬜ Building to WebGL for browser      
⬜ Adding more properties to objects for ease of use in calculations  
⬜ image recognition for windows and edges   
 





### Common Errors
- NullReferenceException : This means you have a null as value instead of the type you acctualy need. To not have this error make sure you check if the variable you want to       access is not (!=) null, and also try to assign a newly created variable with a "new" of that type. For instance: 
  ```c#
  public Dictionary<String,Float> randomDictionary = new Dictionary<String,Float>();
  // This creates an empty dictionary with strings and floats as keys and values.
  ``` 

### Tips
- When starting off take your time to allign a single row of a building so that you can copy an entire floor and paste the rest.
- Make sure there are no errors when trying to build, otherwise the application can not be build. Errors sometimes only arise when trying to build so do this regularly.

### Used Assets
- [Gizmo Manager](https://www.youtube.com/playlist?list=PLPwpt1oIEdwAY_Qo6fczi6qTiUjCMZBW1)  - Youtube playlist
- [Point Cloud Viewer & tools](https://assetstore.unity.com/packages/tools/utilities/point-cloud-viewer-and-tools-16019?aid=1101lGti&utm_source=aff) - Unity Assetstore
- [Excel4Unity](https://github.com/joexi/Excel4Unity) - Github asset
- [HSV-Color_Picker-Unity](https://github.com/judah4/HSV-Color-Picker-Unity) - Github asset
- [Runtime Preview Generator](https://github.com/yasirkula/UnityRuntimePreviewGenerator) - Github asset
- [UnityStandaloneFileBrowser](https://github.com/gkngkc/UnityStandaloneFileBrowser ) - Github asset
- [Quick Outline](https://assetstore.unity.com/packages/tools/particles-effects/quick-outline-115488 ) - Unity Assetstore

## Collaborators
- [Scott Timmemans](https://github.com/KingPungy)
- [Nick Klomp](https://github.com/kllomp)
- [Cor de Kruijf](https://github.com/cordekruijf)
