#version 330
uniform sampler2D shadedBuffer;
uniform sampler2D zBuffer;
uniform sampler2D specularityBuffer;

/*uniform sampler2D albedoBuffer;
uniform sampler2D normalBuffer;
uniform sampler2D voxelLightBuffer;
uniform usampler2D materialBuffer;
uniform sampler2D debugBuffer;*/

uniform sampler2DShadow shadowMap;

uniform sampler2D bloomBuffer;
uniform sampler2D reflectionsBuffer;

uniform sampler2D pauseOverlayTexture;
uniform float pauseOverlayFade;

//uniform samplerCube environmentMap;

in vec2 texCoord;
in vec2 pauseOverlayCoords;

uniform float viewWidth;
uniform float viewHeight;

uniform mat4 projectionMatrix;
uniform mat4 projectionMatrixInv;

uniform mat4 modelViewMatrix;
uniform mat4 modelViewMatrixInv;

uniform mat3 normalMatrix;
uniform mat3 normalMatrixInv;
uniform vec3 camPos;

//Sky data
uniform vec3 sunPos;
uniform float overcastFactor;
uniform float dayTime;

uniform float animationTimer;
uniform float underwater;

uniform float apertureModifier;
uniform vec2 screenViewportSize;

const float gamma = 2.2;
const float gammaInv = 0.45454545454;

const vec4 waterColor = vec4(0.2, 0.4, 0.45, 1.0);

<include ../lib/transformations.glsl>
<include ../lib/shadowTricks.glsl>
<include dither.glsl>
<include ../lib/normalmapping.glsl>
<include ../lib/noise.glsl>
<include ../sky/sky.glsl>

vec4 getDebugShit(vec2 coords);

out vec4 fragColor;

uniform mat4 untranslatedMVInv;
uniform mat4 shadowMatrix;

uniform float shadowVisiblity;
in vec3 eyeDirection;

float bayer2(vec2 a){
    a = floor(a);
    return fract(dot(a,vec2(.5, a.y*.75)));
}

float bayer4(vec2 a)   {return bayer2( .5*a)   * .25     + bayer2(a); }
float bayer8(vec2 a)   {return bayer4( .5*a)   * .25     + bayer2(a); }
float bayer16(vec2 a)  {return bayer4( .25*a)  * .0625   + bayer4(a); }

vec3 ComputeVolumetricLight(vec3 background, vec4 worldSpacePosition, vec3 lightVec, vec3 eyeDirection){
	const int steps = 16;
	const float oneOverSteps = 1.0 / float(steps);

	vec3 startRay = mat3(shadowMatrix) * untranslatedMVInv[3].xyz;
	vec3 endRay = (shadowMatrix * (untranslatedMVInv * worldSpacePosition)).rgb;

	vec3 increment = (startRay - endRay) * oneOverSteps;
	vec3 rayPosition = increment * bayer16(gl_FragCoord.xy) + endRay;

	float weight = clamp(sqrt(dot(increment, increment)), 0.0, 0.25);

	float ray = 0.0;

	for (int i = 0; i < steps; i++){
		vec3 shadowCoord = accuratizeShadow(vec4(rayPosition, 0.0)).rgb + vec3(0.0, 0.0, 0.0001);

		ray += texture(shadowMap, shadowCoord, 0.0);

		rayPosition += increment;
	}
	
	float lDotU = dot(normalize(-lightVec), vec3(0.0, 1.0, 0.0));
	float lDotV = dot(normalize(lightVec), normalize(eyeDirection));
	
	vec3 sunLight_g = sunLightColor;//pow(sunColor, vec3(gamma));
	float sunlightAmount = (ray * clamp(sunPos.y, 0.0, 1.0)) * (oneOverSteps * weight) * (gPhase(lDotV, 0.9) * mCoeff);

	return sunlightAmount * sunLight_g * pi;
}

float poltergeist(vec2 coordinate, float seed)
{
    return fract(sin(dot(coordinate*seed, vec2(12.9898, 78.233)))*43758.5453);
}

vec3 jodieReinhardTonemap(vec3 c){
    float l = dot(c, vec3(0.2126, 0.7152, 0.0722));
    vec3 tc = c / (c + 1.0);

    return mix(c / (l + 1.0), tc, tc);
}

vec4 giMain();

void main() {
	vec2 finalCoords = texCoord;
    vec4 cameraSpacePosition = convertScreenSpaceToCameraSpace(finalCoords, zBuffer);
	
	// Water coordinates distorsion
	finalCoords.x += underwater*sin(finalCoords.x * 50.0 + finalCoords.y * 60.0 + animationTimer * 1.0) / screenViewportSize.x * 5.0;
	finalCoords.y += underwater*cos(finalCoords.y * 60.0 + animationTimer * 1.0) / screenViewportSize.y * 2.0;
	
	// Sampling
	vec4 compositeColor = texture(shadedBuffer, finalCoords);
	
	// Tints pixels blue underwater
	compositeColor = mix(compositeColor, compositeColor * waterColor, underwater);
	
	//Applies reflections
	float reflectionsAmount = texture(specularityBuffer, finalCoords).x;
	
	vec4 reflection = texture(reflectionsBuffer, finalCoords);
	compositeColor.rgb = mix(compositeColor.rgb, reflection.rgb, reflectionsAmount);
	
	//Dynamic reflections
	
	//Volumetric light
	compositeColor.rgb += clamp(ComputeVolumetricLight(compositeColor.rgb, cameraSpacePosition, sunPos, eyeDirection), 0.0, 1.0);

	//GI
	/*
	vec4 gi = giMain();
	compositeColor.rgb = mix(compositeColor.rgb, compositeColor.rgb * gi.rgb, gi.a);
	compositeColor.rgb += gi.rgb * gi.a * 0.05;
	*/
	
	//Applies bloom
	<ifdef doBloom>
	compositeColor.rgb += pow(texture(bloomBuffer, finalCoords).rgb, vec3(gamma)) * pi;
	<endif doBloom>
	
	//Darkens further pixels underwater
	compositeColor = mix(compositeColor, vec4(waterColor.rgb * getSkyColor(dayTime, vec3(0.0, -1.0, 0.0)), 1.0), clamp(length(cameraSpacePosition) / 32.0, 0.0, 1.0) * underwater);
	
	// Eye adapatation
	compositeColor *= apertureModifier;
	
	//Gamma-corrects stuff
	compositeColor.rgb = pow(compositeColor.rgb, vec3(gammaInv));
	
	//Dither the final pixelcolour
	vec3 its2 = compositeColor.rgb;
    vec3 rnd2 = screenSpaceDither( gl_FragCoord.xy );
    compositeColor.rgb = its2 + rnd2.xyz;
	
	//Applies pause overlay
	vec3 overlayColor = texture(pauseOverlayTexture, pauseOverlayCoords).rgb;
	overlayColor = vec3(
	
	( mod(gl_FragCoord.x + gl_FragCoord.y, 2.0) * 0.45 + 0.55 )
	* 
	( poltergeist(gl_FragCoord.xy, animationTimer) * 0.15 + 0.85 )
	
	);
	compositeColor.rgb *= mix(vec3(1.0), overlayColor, clamp(pauseOverlayFade, 0.0, 1.0));

	compositeColor.rgb = pow(jodieReinhardTonemap(compositeColor.rgb * 5.0), vec3(gamma));
	
	//Ouputs
	fragColor = compositeColor;
	
	//Debug flag
	<ifdef debugGBuffers>
	//fragColor = getDebugShit(texCoord);
	<endif debugGBuffers>
}

//Draws divided screen with debug buffers
/*vec4 getDebugShit(vec2 coords)
{
	vec2 sampleCoords = coords;
	sampleCoords.x = mod(sampleCoords.x, 0.5);
	sampleCoords.y = mod(sampleCoords.y, 0.5);
	sampleCoords *= 2.0;
	
	vec4 shit = vec4(0.0);
	if(coords.x > 0.5)
	{
		if(coords.y > 0.5)
			shit = pow(texture(shadedBuffer, sampleCoords, 0.0), vec4(gammaInv));
		else
			shit = texture(normalBuffer, sampleCoords);
	}
	else
	{
		if(coords.y > 0.5)
		{
			shit = texture(albedoBuffer, sampleCoords);
			shit += (1.0-shit.a) * vec4(1.0, 0.0, 1.0, 1.0);
		}
		else
		{
			shit = vec4(texture(voxelLightBuffer, sampleCoords).xy, texture(specularityBuffer, sampleCoords).r, 1.0);
			
			<ifdef dynamicGrass>
			
			shit = texture(debugBuffer, sampleCoords, 80.0);
			shit = pow(texture(debugBuffer, sampleCoords, 0.0), vec4(gammaInv));
			<endif dynamicGrass>
			shit = vec4(texture(shadowMap, vec3(sampleCoords, 0.0)), 0.0, 0.0, 1.0);
		}
	}
	shit.a = 1.0;
	return shit;
}*/