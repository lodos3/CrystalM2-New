# Original armour colour layers

g02–g99 retain their original source mapping and nine palette groups. Each source library is analysed at 32 fixed native frames (eight idle and eight attack facings for each of two body banks). The histogram excludes isolated skin and extreme black/white; deterministic weighted clustering learns up to four chromatic families. The same model is reused across every pose and variant of that source. Actual materials are not inferred as physically measured metal or cloth.

Four complementary BGRA layers map the source's learned colour families to four intentionally contrasting palette colours. Each pixel belongs to exactly one layer, preserving original alpha and location. Dark contours and near-white highlights remain unchanged. The original head/hands layer is drawn last. Frost and amethyst have a selected-layer shimmer shader; teal uses a selected-layer holographic scan. These effects preserve alpha and do not affect the skin. The existing global scene tint, filtering and greyscale remain active. Generated IDs now take precedence over stale GArmour exports for g02–g99; fallback remains available if generation fails. g01 is unchanged.

The cache still has a 32 MiB texture payload budget and 192-entry limit, but may now upload up to five textures per frame. Source colour model construction occurs on first use; profile performance and filtered edges require visual testing.

Export one design with the existing --live-one command. Exports are gNN_HeadHands.Lib, gNN_Surface.Lib (combined recolour) and gNN_Layer0.Lib through gNN_Layer3.Lib. Exported colours are static; animated shader effects are runtime-only. Use a fresh directory to avoid overwriting previous exports.

Side-facing physical wings (direction indices 2 and 6): raise the previous constrained sockets by 1–2 pixels, reduce vertical transverse span symmetrically, shorten feather height below the artwork root by 15%, suppress legacy lift and retain bounded pitch/yaw. The upper crown remains uncompressed. Other directions and orbiting auras retain their original behaviour. The mesh still pins each root to a shared strip edge. Numerical limits are initial tuning, not a measurement of the screenshot or proof of correct occlusion.

Validation: Tests/ModernRendererTests includes colour clustering, disjoint alpha conservation, source shading endpoints, skin preservation and mirrored profile placement bounds. Real client screenshots, all animation directions and GPU profiling are still required.
