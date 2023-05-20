
## Proposing the Use of Sprite for Character Rendering in RMU

I would like to explain in more detail why I suggest using Sprites instead of a combination of Canvas+Image in Unity. This is a very basic description. I tried to understand why RMU uses Canvas+Image in the first place and then explained why we should not do this.

1.  ### Coordinate System   
UI elements in Unity use a 'Pixel' position, though this might not be the actual screen pixel. It could be a pixel position relative to the reference resolution. In RMU, it's assumed that the CharacterGraphic is reused, so they prefer to use 'Pixel' positioning to simplify the layout. For instance, the battle scene, which mainly consists of UI elements, including the characters, is more comfortable to arrange using Canvas+Image. However, on the tilemap, they already use the World Coordinate system, which is unrelated to pixels. In RMU, it is set as 1 unit equals 1 tile, which is a good practice. Using UI+Canvas offers no real advantage here.

2. ### Rendering Speed  
Generally speaking, faster rendering in Unity equals fewer draw calls. Therefore, the batch number should be lower.  

Unity renders UI elements in a separate rendering pipeline. Typically, UI elements sharing the same textures can be rendered in the same batch (1 draw call). However, this only works if they are under the same Canvas component (children of a common GameObject which contains one Canvas). In RMU, the Character GameObject contains its own Canvas+Image, leading to each character creating its own batch.  

As a result, even characters that share textures will have no batching, which can degrade performance. I created a screenshot for comparison. There are 4 dragons rendered with Canvas+Image, similar to RMU's method. This results in a total of 5 batches: (1 for each dragon + 1 for the background). 
![Use Sprite](https://cdn.discordapp.com/attachments/943317683481505882/1109159904092106902/Unity_9xDnjdvw0O.png)

On the screenshot, the same four dragons are rendered using only Sprites, which reduces the number of batches to two: 1 for all four dragons together + 1 for the background.
![Use Canvas like RMU](https://cdn.discordapp.com/attachments/943317683481505882/1109159904582828133/Unity_0qZQg73rTD.png)

I know that different characters may use different textures, but there are situations where characters share textures. Furthermore, if we use Sprites, we could pack characters' textures into several texture atlases for future optimization.  

Take the first map in the sample project as an example; it has over 300 rendering batches. This high number can lead to performance issues on mobile devices.  

Thanks @agoaj for the additional information, moving around UI elements is also bad for performance.

3. ### Shader Compatibility 
Since UI has its own rendering pipeline, its shaders differ from those used for Sprites. Many 2D effect shaders available from the Unity Asset Store can only be used on Sprites. UI shaders have more restrictions and can't easily port from the Sprite's shaders directly. 

## Conclusion
Using UI to render characters limits future customizations. I hope this explanation clarifies my earlier point. I believe it's essential to understand the impact of design choices in Unity on performance and future extensibility.

To change the Character using Sprite, you might need to modify these code files in RMU:  

- Assets\RPGMaker\Codebase\Runtime\Map\Component\Character\CharacterGraphic.cs
- Assets\RPGMaker\Codebase\Runtime\Map\Component\Character\CharacterOnMap.cs
- Assets\RPGMaker\Codebase\Runtime\Common\Component\Hud\Character\CharacterAnimation.cs  

I cannot publicly share any modified code until RMU has a public repository and starts accepting Pull Requests.