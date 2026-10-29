// Brief 077 (Latch): what the DK leaf pass copies (DK_LeafForward, DK_LeafGBuffer, DK_LeafDepthNormals) share: the fragment's facing for
// _DK_TWO_SIDED, as Shader Graph carries it (FRONT_FACE_SEMANTIC in the fragment stage's copy of Varyings only, so the vertex stage's output
// struct is unchanged). Included only from their keyword branch, never by a plain variant.
#ifndef DK_LEAF_INCLUDED
#define DK_LEAF_INCLUDED

#if defined(_DK_TWO_SIDED) && defined(SHADER_STAGE_FRAGMENT)
    #define DK_LEAF_FACE_FIELD FRONT_FACE_TYPE dkFrontFace : FRONT_FACE_SEMANTIC;
    #define DK_LEAF_FACE_SIGN(input) IS_FRONT_VFACE(input.dkFrontFace, half(1.0), half(-1.0))
#else
    #define DK_LEAF_FACE_FIELD
    #define DK_LEAF_FACE_SIGN(input) half(1.0)
#endif

#endif
