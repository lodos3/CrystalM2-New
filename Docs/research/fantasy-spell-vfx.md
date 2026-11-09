# Fantasy spell VFX research

Crystal’s reference set covers 137 distinct, individually checked lessons. These are standalone walkthroughs, instructional video lessons, and practical graphics chapters. Each counted page returned HTTP 200 with lesson content on 9 November 2026. The findings synthesize a focused set of techniques. The numbered list is the broader reading collection, with manuals, style guides, and shader samples listed separately.

The starting Wizard change in [6691d0f](https://github.com/lodos3/CrystalM2-New/commit/6691d0ffa59ab0acd6a933b95b0c180fab4bc4c1) uses directional effects in place of lightning and storm backdrops. The same principle applies to Taoist effects. The primary silhouette identifies the spell. Smaller shapes and emitted light show where it is moving, hitting or fading.

## Findings and applications

| Topic | Source finding | Application in Crystal |
| --- | --- | --- |
| Gameplay readability | Riot treats shape, value, colour, and timing as separate controls. The visual peak should communicate the spell’s main gameplay moment. The quieter outro can change hue, saturation, and opacity. [Riot VFX Style Guide, sections III–VI](https://nexus.leagueoflegends.com/wp-content/uploads/2017/10/VFX_Styleguide_final_public_hidpjqwx7lqyx0pjj3ss.pdf) | Use a different silhouette for a heal, an armour rune and a summon. Keep the established cast and hit windows. Give the brightest few particles to the point of ignition or arrival. |
| Particle composition | Epic’s sparks exercise builds separate emitters for the spark shower, centre spark and rising smoke. It varies the burst’s velocity, size and rotation, with a separate curve for its opacity. The firework trail lesson uses one stage to trigger the next. [Niagara sparks](https://dev.epicgames.com/documentation/en-us/unreal-engine/how-to-create-a-sparks-effect-in-niagara-for-unreal-engine), [Niagara firework trail](https://dev.epicgames.com/documentation/fortnite/creating-fireworks-3-building-the-firework-trail-in-unreal-editor-for-fortnite) | Author large, medium and small elements with distinct paths and fade curves. Healing uses clusters of pinpricks around focal glints and low-opacity halos. Smoke starts as falling wisps and leaves thin spreading mist at contact. |
| Burn direction | The original author’s dissolve shader uses spatial noise and a directional sweep to move a narrow hot edge through a sprite. The Book of Shaders shows how interpolated noise adds coherent variation to the shape and how shaping curves soften transitions. [Dissolve with glowing burn edge](https://godotshaders.com/shader/dissolve-with-glowing-burn-edge/), [Shaping functions](https://thebookofshaders.com/05/), [Noise](https://thebookofshaders.com/11/) | Keep the ward at its final scale. Advance ignition from one corner along the glyph and offset nearby strokes slightly so the front is irregular. Taper the active edge, attach the hottest flame tongues to it and let the strokes behind it cool. Emit one ward at the cast location. |
| Coherent motion | Godot’s turbulence guide applies noise to an existing movement field. Daniel Shiffman’s examples separate position, velocity and acceleration, with independent emitters and forces. [Particle turbulence](https://docs.godotengine.org/en/stable/tutorials/3d/particles/turbulence.html), [Forces](https://natureofcode.com/forces/), [Particle systems](https://natureofcode.com/particles/) | Seed each mote once. Vary speed, phase, shape, and lifetime per particle. Carry the smoke’s light with individual puffs as they descend, then spread mist tangentially after landing. Move the Energy Shield pulse along the contour and give its upward-drifting sparks separate ages. |
| Volume and depth | NVIDIA demonstrates fading particle alpha near depth intersections. Jasper Flick shows a sprite workflow for soft intersections, flipbook blending and near fades. Godot distinguishes global and emitter-local particles. [High-Speed, Off-Screen Particles, §23.4](https://developer.nvidia.com/gpugems/gpugems3/part-iv-image-effects/chapter-23-high-speed-screen-particles), [Catlike Coding particles](https://catlikecoding.com/unity/tutorials/custom-srp/particles/), [Godot 2D particles, local coordinates](https://docs.godotengine.org/en/stable/tutorials/2d/particle_systems_2d.html#local-coords) | Assign depth from the particle’s ground root before adding elevation. Use rear wisps, a bright contour in front and contact haze on the floor. Crystal stores scene row and partial sprite coverage, so the adaptation uses that coverage where the sources use continuous 3D depth. |
| Soft shapes and texture detail | Loop and Blinn evaluate Bézier boundaries analytically in the pixel shader. Flow-map UV distortion changes texture motion continuously and can blend over a cycle. [Rendering Vector Art on the GPU](https://developer.nvidia.com/gpugems/gpugems3/part-iv-image-effects/chapter-25-rendering-vector-art-gpu), [Texture Distortion](https://catlikecoding.com/unity/tutorials/flow/texture-distortion/) | Use a filtered high-resolution coverage mask or a smoothly tapered path for glyphs and crescents. Preserve wispy edges by blending consecutive clip frames. Add variation at two scales, with a slow broad curl and faster narrow detail. |
| Light and blend | NVIDIA isolates luminous regions, blurs their contribution and composites it over the image. Catlike Coding builds a progressive blur chain and a soft-knee brightness threshold. [Real-Time Glow](https://developer.nvidia.com/gpugems/gpugems/part-iv-image-processing/chapter-21-real-time-glow), [Catlike Coding post processing](https://catlikecoding.com/unity/tutorials/custom-srp/post-processing/), [Catlike Coding bloom](https://catlikecoding.com/unity/tutorials/advanced-rendering/bloom/) | Put smoke into alpha coverage. Keep hot tips and spark cores in the emissive pass. Give only selected elements shaped scene light. Tune the core at default F12 settings and then test the glow independently. |
| Performance at play zoom | NVIDIA treats smoke and fog as low-frequency detail that can tolerate a coarser target. The mixed-resolution technique rerenders near detected edges at full resolution, since reduced-resolution depth can introduce halos and reduced-resolution colour can break up small features. [High-Speed, Off-Screen Particles, §§23.2–23.7](https://developer.nvidia.com/gpugems/gpugems3/part-iv-image-effects/chapter-23-high-speed-screen-particles) | Keep the core, glyph and sparkles sharp at native resolution. If profiling later warrants a cheaper soft halo or mist pass, check intersections at native scale and preserve scene coverage where the passes meet. |

## Working checks for future spells

1. Compare the original cue at anticipation, first contact, peak and dissipation. Check the target tile and the body anchor independently. Read the shape at gameplay zoom on bright and dark maps.
2. Test depth with a character in front and behind the effect. Check ground shadows, canopy edges, and a moving caster. Sprite coverage and projected elevation should agree even when the visual bloom is strong.
3. Check fine highlights at normal and reduced display scale. Cores should survive filtering, tapers should remain continuous, and a veil should keep visible gaps between wisps.
4. Validate single cast, overlapping casts and cleanup. Observe the target packet path separately from the one-shot cast effect. Check start and end boundaries, stable random seeds, repeat phases, and stale emitters after a map change.
5. Use both particle-only and fully composited views. Review the F12 glow controls, the visible colour layers, and the map-light projection separately before tuning overall brightness.

The local implementation uses [SpellParticleFrame](../../Client/MirScenes/SpellParticleFrame.cs) for seeded placement, scale and depth. [SpellParticleRenderer](../../Client/Rendering/SpellParticleRenderer.cs) owns layer blending and applies [World.hlsl](../../Client/Data/Shaders/World.hlsl) coverage. [MapControl.SpellParticles](../../Client/MirScenes/MapControl.SpellParticles.cs) ties those layers to world positions and graphics controls. [WindowsRendererTests](../../Tests/WindowsRendererTests/Program.cs) exercises Direct3D 11 shader behaviour and cue geometry.

The Taoist pass gives the summons local glints that follow their moving stars or talismans. Curse puts smoke and sparks around its strokes to leave the glyph legible. Energy Shield sends a travelling brightness pulse along a body-anchored contour. [Taoist ritual effects](../../Client/MirScenes/TaoistSpellEffects.Rituals.cs), [Taoist fields and auras](../../Client/MirScenes/TaoistSpellEffects.Fields.cs)

Flaming Sword uses rear and front tongues on distinct ground rows so the target can sit inside the fire, with elevation taken from each flame’s height. [Warrior spell effects](../../Client/MirScenes/WarriorSpellEffects.cs)

## Additional authored examples

- Use the adjustable [Procedural Magic Circle](https://godotshaders.com/shader/procedural-magic-circle/) as a study of rings, radial ticks, orbiters and timed drawing.
- Compare moving local ripples and rim treatment in [Shield with impact waves](https://godotshaders.com/shader/shield-with-impact-waves/). A projected animated contour can use the same hierarchy with a smaller rendering cost in Crystal.
- Look at the separate twist, colour, alpha, and tail motions in [Stylized BOTW Fire](https://godotshaders.com/shader/stylized-botw-fire/) and layering in [2.5D smoke and fire with Niagara Fluids](https://dev.epicgames.com/community/learning/tutorials/5JbZ/unreal-engine-2-5d-smoke-and-fire-with-niagara-fluids).
- Use [Clarity in League](https://www.leagueoflegends.com/en-us/news/dev/clarity-in-league/) and [Behind the Scenes of VFX Updates](https://www.leagueoflegends.com/en-au/news/dev/dev-behind-the-scenes-of-vfx-updates/) to review silhouette, colour contrast, affected area and visual priority across a whole class.

## Engine reference manuals

- Look up individual emitter controls in [Godot particle process material properties](https://docs.godotengine.org/en/stable/tutorials/3d/particles/process_material_properties.html).
- Look up shader input equivalents in [Converting GLSL to Godot shaders](https://docs.godotengine.org/en/stable/tutorials/shaders/converting_glsl_to_godot_shaders.html).

## Particle choreography, trails and fantasy effects

Work through emission and lifetime first, then give each emitter a job in the effect. The firework sequence separates the carrier, trail, sprite burst and secondary ribbons into five practical lessons.

1. [Beam Effect](https://dev.epicgames.com/documentation/unreal-engine/how-to-create-a-beam-effect-in-niagara-for-unreal-engine) · Epic Games
2. [Dark Smoke](https://dev.epicgames.com/documentation/unreal-engine/how-to-create-a-dark-smoke-effect-in-niagara-for-unreal-engine) · Epic Games
3. [GPU Sprite Effect](https://dev.epicgames.com/documentation/unreal-engine/how-to-create-a-gpu-sprite-effect-in-niagara-for-unreal-engine) · Epic Games
4. [Mesh Balloons](https://dev.epicgames.com/documentation/unreal-engine/how-to-use-a-solid-mesh-to-create-a-balloon-effect-in-niagara-for-unreal-engine) · Epic Games
5. [Particle Lights](https://dev.epicgames.com/documentation/unreal-engine/how-to-create-particle-effects-that-emit-light-in-niagara-for-unreal-engine) · Epic Games
6. [Ribbon Effect](https://dev.epicgames.com/documentation/unreal-engine/how-to-create-a-ribbon-effect-in-niagara-for-unreal-engine) · Epic Games
7. [Sprite Smoke](https://dev.epicgames.com/documentation/unreal-engine/how-to-create-a-smoke-effect-using-sprite-particles-in-niagara-for-unreal-engine) · Epic Games
8. [Steam Effect](https://dev.epicgames.com/documentation/unreal-engine/how-to-create-a-steam-effect-in-niagara-for-unreal-engine) · Epic Games
9. [Sparks Effect](https://dev.epicgames.com/documentation/unreal-engine/how-to-create-a-sparks-effect-in-niagara-for-unreal-engine) · Epic Games
10. [1. Create the Firework Material and Niagara Emitter](https://dev.epicgames.com/documentation/fortnite/creating-fireworks-1-create-the-firework-material-and-niagara-emitter-in-unreal-editor-for-fortnite) · Epic Games
11. [2. Building the Firework Head](https://dev.epicgames.com/documentation/fortnite/2-building-the-firework-head-in-unreal-editor-for-fortnite) · Epic Games
12. [3. Building the Firework Trail](https://dev.epicgames.com/documentation/fortnite/creating-fireworks-3-building-the-firework-trail-in-unreal-editor-for-fortnite) · Epic Games
13. [4. Making the First Firework Explosion](https://dev.epicgames.com/documentation/fortnite/creating-fireworks-4-making-the-first-firework-explosion-in-unreal-editor-for-fortnite) · Epic Games
14. [5. Making the Second Firework Explosion](https://dev.epicgames.com/documentation/fortnite/creating-fireworks-5-making-the-second-firework-explosion-in-unreal-editor-for-fortnite) · Epic Games
15. [Epic Niagara quick start](https://dev.epicgames.com/documentation/en-us/unreal-engine/quick-start-for-niagara-effects-in-unreal-engine) · Epic Games
16. [Epic Unreal fire trap effect](https://dev.epicgames.com/documentation/en-us/unreal-engine/at12-adding-visual-effects-to-your-game) · Epic Games
17. [Riot Art Education, Episode 7, Visual Effects](https://www.riotgames.com/en/artedu/visual-effects) · Riot Games
18. [Godot 2D particle systems](https://docs.godotengine.org/en/stable/tutorials/2d/particle_systems_2d.html) · Godot
19. [Creating a 3D particle system](https://docs.godotengine.org/en/stable/tutorials/3d/particles/creating_a_3d_particle_system.html) · Godot
20. [Particle sub-emitters](https://docs.godotengine.org/en/stable/tutorials/3d/particles/subemitters.html) · Godot
21. [3D Particle trails](https://docs.godotengine.org/en/stable/tutorials/3d/particles/trails.html) · Godot
22. [Particle turbulence](https://docs.godotengine.org/en/stable/tutorials/3d/particles/turbulence.html) · Godot
23. [3D Particle attractors](https://docs.godotengine.org/en/stable/tutorials/3d/particles/attractors.html) · Godot
24. [3D Particle collisions](https://docs.godotengine.org/en/stable/tutorials/3d/particles/collision.html) · Godot
25. [Complex emission shapes](https://docs.godotengine.org/en/stable/tutorials/3d/particles/complex_shapes.html) · Godot
26. [Particles](https://catlikecoding.com/unity/tutorials/custom-srp/particles/) · Catlike Coding
27. [Rendering Sprites](https://learnopengl.com/In-Practice/2D-Game/Rendering-Sprites) · LearnOpenGL
28. [Particles](https://learnopengl.com/In-Practice/2D-Game/Particles) · LearnOpenGL

## Procedural shaders, shapes and textures

Use shaping functions and texture sampling to control edge coverage. Study seeded fields, derivatives and coherent variation before adding more geometry or brightness to an effect.

29. [Uniforms](https://thebookofshaders.com/03/) · The Book of Shaders
30. [Shaping functions](https://thebookofshaders.com/05/) · The Book of Shaders
31. [Colors](https://thebookofshaders.com/06/) · The Book of Shaders
32. [Shapes](https://thebookofshaders.com/07/) · The Book of Shaders
33. [Matrices](https://thebookofshaders.com/08/) · The Book of Shaders
34. [Patterns](https://thebookofshaders.com/09/) · The Book of Shaders
35. [Random](https://thebookofshaders.com/10/) · The Book of Shaders
36. [Noise](https://thebookofshaders.com/11/) · The Book of Shaders
37. [Cellular noise](https://thebookofshaders.com/12/) · The Book of Shaders
38. [Fractional brownian motion](https://thebookofshaders.com/13/) · The Book of Shaders
39. [Hashing](https://catlikecoding.com/unity/tutorials/pseudorandom-noise/hashing/) · Catlike Coding
40. [Hashing Space](https://catlikecoding.com/unity/tutorials/pseudorandom-noise/hashing-space/) · Catlike Coding
41. [Value Noise](https://catlikecoding.com/unity/tutorials/pseudorandom-noise/value-noise/) · Catlike Coding
42. [Perlin Noise](https://catlikecoding.com/unity/tutorials/pseudorandom-noise/perlin-noise/) · Catlike Coding
43. [Noise Variants](https://catlikecoding.com/unity/tutorials/pseudorandom-noise/noise-variants/) · Catlike Coding
44. [Voronoi Noise](https://catlikecoding.com/unity/tutorials/pseudorandom-noise/voronoi-noise/) · Catlike Coding
45. [Simplex Noise](https://catlikecoding.com/unity/tutorials/pseudorandom-noise/simplex-noise/) · Catlike Coding
46. [Simplex Derivatives](https://catlikecoding.com/unity/tutorials/pseudorandom-surfaces/simplex-derivatives/) · Catlike Coding
47. [Perlin Derivatives](https://catlikecoding.com/unity/tutorials/pseudorandom-surfaces/perlin-derivatives/) · Catlike Coding
48. [Voronoi Derivatives](https://catlikecoding.com/unity/tutorials/pseudorandom-surfaces/voronoi-derivatives/) · Catlike Coding
49. [Surface Flow](https://catlikecoding.com/unity/tutorials/pseudorandom-surfaces/surface-flow/) · Catlike Coding
50. [Matrices](https://catlikecoding.com/unity/tutorials/rendering/part-1/) · Catlike Coding
51. [Shader Fundamentals](https://catlikecoding.com/unity/tutorials/rendering/part-2/) · Catlike Coding
52. [Combining Textures](https://catlikecoding.com/unity/tutorials/rendering/part-3/) · Catlike Coding
53. [Bumpiness](https://catlikecoding.com/unity/tutorials/rendering/part-6/) · Catlike Coding
54. [Parallax](https://catlikecoding.com/unity/tutorials/rendering/part-20/) · Catlike Coding
55. [Surface Displacement](https://catlikecoding.com/unity/tutorials/advanced-rendering/surface-displacement/) · Catlike Coding
56. [Triplanar Mapping](https://catlikecoding.com/unity/tutorials/advanced-rendering/triplanar-mapping/) · Catlike Coding
57. [Your first 2D shader](https://docs.godotengine.org/en/stable/tutorials/shaders/your_first_shader/your_first_2d_shader.html) · Godot
58. [Your first 3D shader](https://docs.godotengine.org/en/stable/tutorials/shaders/your_first_shader/your_first_3d_shader.html) · Godot
59. [Your second 3D shader](https://docs.godotengine.org/en/stable/tutorials/shaders/your_first_shader/your_second_3d_shader.html) · Godot
60. [Using VisualShaders](https://docs.godotengine.org/en/stable/tutorials/shaders/visual_shaders.html) · Godot
61. [Using compute shaders](https://docs.godotengine.org/en/stable/tutorials/shaders/compute_shaders.html) · Godot
62. [Screen-reading shaders](https://docs.godotengine.org/en/stable/tutorials/shaders/screen-reading_shaders.html) · Godot
63. [Shaders](https://learnopengl.com/Getting-started/Shaders) · LearnOpenGL
64. [Textures](https://learnopengl.com/Getting-started/Textures) · LearnOpenGL
65. [Transformations](https://learnopengl.com/Getting-started/Transformations) · LearnOpenGL
66. [Coordinate Systems](https://learnopengl.com/Getting-started/Coordinate-Systems) · LearnOpenGL
67. [Advanced GLSL](https://learnopengl.com/Advanced-OpenGL/Advanced-GLSL) · LearnOpenGL
68. [Geometry Shader](https://learnopengl.com/Advanced-OpenGL/Geometry-Shader) · LearnOpenGL
69. [Chapter 25. Rendering Vector Art on the GPU](https://developer.nvidia.com/gpugems/gpugems3/part-iv-image-effects/chapter-25-rendering-vector-art-gpu) · GPU Gems

## Motion physics and depth

Design trajectories and lifetime in world space. These lessons cover independent emitters, gravity, drag, trails, collision and visibility rules that can be applied to an isometric projection.

70. [Texture Distortion](https://catlikecoding.com/unity/tutorials/flow/texture-distortion/) · Catlike Coding
71. [Directional Flow](https://catlikecoding.com/unity/tutorials/flow/directional-flow/) · Catlike Coding
72. [Waves](https://catlikecoding.com/unity/tutorials/flow/waves/) · Catlike Coding
73. [Looking Through Water](https://catlikecoding.com/unity/tutorials/flow/looking-through-water/) · Catlike Coding
74. [Randomness](https://natureofcode.com/random/) · The Nature of Code
75. [Vectors](https://natureofcode.com/vectors/) · The Nature of Code
76. [Forces](https://natureofcode.com/forces/) · The Nature of Code
77. [Oscillation](https://natureofcode.com/oscillation/) · The Nature of Code
78. [Particle Systems](https://natureofcode.com/particles/) · The Nature of Code
79. [Autonomous Agents](https://natureofcode.com/autonomous-agents/) · The Nature of Code
80. [Physics Libraries](https://natureofcode.com/physics-libraries/) · The Nature of Code
81. [Fractals](https://natureofcode.com/fractals/) · The Nature of Code
82. [Depth testing](https://learnopengl.com/Advanced-OpenGL/Depth-testing) · LearnOpenGL
83. [Stencil testing](https://learnopengl.com/Advanced-OpenGL/Stencil-testing) · LearnOpenGL
84. [Blending](https://learnopengl.com/Advanced-OpenGL/Blending) · LearnOpenGL
85. [Instancing](https://learnopengl.com/Advanced-OpenGL/Instancing) · LearnOpenGL
86. [Collision detection](https://learnopengl.com/In-Practice/2D-Game/Collisions/Collision-detection) · LearnOpenGL
87. [Collision resolution](https://learnopengl.com/In-Practice/2D-Game/Collisions/Collision-resolution) · LearnOpenGL
88. [Frustum Culling](https://learnopengl.com/Guest-Articles/2021/Scene/Frustum-Culling) · LearnOpenGL
89. [Transparency](https://catlikecoding.com/unity/tutorials/rendering/part-11/) · Catlike Coding
90. [Semitransparent Shadows](https://catlikecoding.com/unity/tutorials/rendering/part-12/) · Catlike Coding
91. [GPU Instancing](https://catlikecoding.com/unity/tutorials/rendering/part-19/) · Catlike Coding
92. [Chapter 23. High-Speed, Off-Screen Particles](https://developer.nvidia.com/gpugems/gpugems3/part-iv-image-effects/chapter-23-high-speed-screen-particles) · GPU Gems
93. [Chapter 27. Motion Blur as a Post-Processing Effect](https://developer.nvidia.com/gpugems/gpugems3/part-iv-image-effects/chapter-27-motion-blur-post-processing-effect) · GPU Gems
94. [Chapter 30. Real-Time Simulation and Rendering of 3D Fluids](https://developer.nvidia.com/gpugems/gpugems3/part-v-physics-simulation/chapter-30-real-time-simulation-and-rendering-3d-fluids) · GPU Gems
95. [Fast Fluid Dynamics Simulation on the GPU](https://developer.nvidia.com/gpugems/gpugems/part-vi-beyond-triangles/chapter-38-fast-fluid-dynamics-simulation-gpu) · GPU Gems
96. [Volume Rendering Techniques](https://developer.nvidia.com/gpugems/gpugems/part-vi-beyond-triangles/chapter-39-volume-rendering-techniques) · GPU Gems

## Lighting, bloom and compositing

These lessons separate visible sprites, scene lights and the blur used for glow. They also cover the pipeline decisions that affect the result at normal play zoom, including colour space, sampling and render resolution.

97. [Colors](https://learnopengl.com/Lighting/Colors) · LearnOpenGL
98. [Basic Lighting](https://learnopengl.com/Lighting/Basic-Lighting) · LearnOpenGL
99. [Lighting maps](https://learnopengl.com/Lighting/Lighting-maps) · LearnOpenGL
100. [Light casters](https://learnopengl.com/Lighting/Light-casters) · LearnOpenGL
101. [Multiple lights](https://learnopengl.com/Lighting/Multiple-lights) · LearnOpenGL
102. [Framebuffers](https://learnopengl.com/Advanced-OpenGL/Framebuffers) · LearnOpenGL
103. [Anti Aliasing](https://learnopengl.com/Advanced-OpenGL/Anti-Aliasing) · LearnOpenGL
104. [Gamma Correction](https://learnopengl.com/Advanced-Lighting/Gamma-Correction) · LearnOpenGL
105. [Shadow Mapping](https://learnopengl.com/Advanced-Lighting/Shadows/Shadow-Mapping) · LearnOpenGL
106. [Point Shadows](https://learnopengl.com/Advanced-Lighting/Shadows/Point-Shadows) · LearnOpenGL
107. [Normal Mapping](https://learnopengl.com/Advanced-Lighting/Normal-Mapping) · LearnOpenGL
108. [HDR](https://learnopengl.com/Advanced-Lighting/HDR) · LearnOpenGL
109. [Bloom](https://learnopengl.com/Advanced-Lighting/Bloom) · LearnOpenGL
110. [Deferred Shading](https://learnopengl.com/Advanced-Lighting/Deferred-Shading) · LearnOpenGL
111. [Postprocessing](https://learnopengl.com/In-Practice/2D-Game/Postprocessing) · LearnOpenGL
112. [Weighted Blended](https://learnopengl.com/Guest-Articles/2020/OIT/Weighted-Blended) · LearnOpenGL
113. [Phys. Based Bloom](https://learnopengl.com/Guest-Articles/2022/Phys.-Based-Bloom) · LearnOpenGL
114. [Area Lights](https://learnopengl.com/Guest-Articles/2022/Area-Lights) · LearnOpenGL
115. [Draw Calls](https://catlikecoding.com/unity/tutorials/custom-srp/draw-calls/) · Catlike Coding
116. [Point and Spot Lights](https://catlikecoding.com/unity/tutorials/custom-srp/point-and-spot-lights/) · Catlike Coding
117. [Point and Spot Shadows](https://catlikecoding.com/unity/tutorials/custom-srp/point-and-spot-shadows/) · Catlike Coding
118. [Post Processing](https://catlikecoding.com/unity/tutorials/custom-srp/post-processing/) · Catlike Coding
119. [HDR](https://catlikecoding.com/unity/tutorials/custom-srp/hdr/) · Catlike Coding
120. [Color Grading](https://catlikecoding.com/unity/tutorials/custom-srp/color-grading/) · Catlike Coding
121. [Render Scale](https://catlikecoding.com/unity/tutorials/custom-srp/render-scale/) · Catlike Coding
122. [FXAA](https://catlikecoding.com/unity/tutorials/custom-srp/fxaa/) · Catlike Coding
123. [Bloom](https://catlikecoding.com/unity/tutorials/advanced-rendering/bloom/) · Catlike Coding
124. [FXAA](https://catlikecoding.com/unity/tutorials/advanced-rendering/fxaa/) · Catlike Coding
125. [The First Light](https://catlikecoding.com/unity/tutorials/rendering/part-4/) · Catlike Coding
126. [Multiple Lights](https://catlikecoding.com/unity/tutorials/rendering/part-5/) · Catlike Coding
127. [Shadows](https://catlikecoding.com/unity/tutorials/rendering/part-7/) · Catlike Coding
128. [Deferred Shading](https://catlikecoding.com/unity/tutorials/rendering/part-13/) · Catlike Coding
129. [Fog](https://catlikecoding.com/unity/tutorials/rendering/part-14/) · Catlike Coding
130. [Deferred Lights](https://catlikecoding.com/unity/tutorials/rendering/part-15/) · Catlike Coding
131. [Using a SubViewport as a texture](https://docs.godotengine.org/en/stable/tutorials/shaders/using_viewport_as_texture.html) · Godot
132. [Custom post-processing](https://docs.godotengine.org/en/stable/tutorials/shaders/custom_postprocessing.html) · Godot
133. [Advanced post-processing](https://docs.godotengine.org/en/stable/tutorials/shaders/advanced_postprocessing.html) · Godot
134. [Chapter 13. Volumetric Light Scattering as a Post-Process](https://developer.nvidia.com/gpugems/gpugems3/part-ii-light-and-shadows/chapter-13-volumetric-light-scattering-post-process) · GPU Gems
135. [Chapter 24. The Importance of Being Linear](https://developer.nvidia.com/gpugems/gpugems3/part-iv-image-effects/chapter-24-importance-being-linear) · GPU Gems
136. [Chapter 40. Incremental Computation of the Gaussian](https://developer.nvidia.com/gpugems/gpugems3/part-vi-gpu-computing/chapter-40-incremental-computation-gaussian) · GPU Gems
137. [Real-Time Glow](https://developer.nvidia.com/gpugems/gpugems/part-iv-image-processing/chapter-21-real-time-glow) · GPU Gems
