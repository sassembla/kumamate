# kumamate
figma to GUI layout support tool for Unity.

kumamate aims "apply/update parameter from figma to Unity continuously easy".

## only apply position, layout & parameters from figma
* kumamate helps apply the parameters of figma to aleady exists Unity GUI objects.
* position, size, color, text and font settings will be applied.
* never generate UI objects from figma data.

## installation
use unitypackage from Releases.

## usage
see movie.
https://user-images.githubusercontent.com/944441/127865919-2ca5c957-dac5-411f-b372-593c118c4f8b.mov

1. get your figma file shared link of what you want to set layout to Unity object.
2. open kumamate window. 
3. set shared link url.
4. hit "Get File Data From Figma" button.
5. your browser will open figma page and ask you "allow kumamate to access your figma file". allow if you want.
6. now the figma data is in UnityEditor. hit "一番目のファイルを読む" button, then you can see the figma layout layer window called "KumaUILayoutTargetWindow".
7. drag & drop your UI object to window. your UI object will be applied figma parameter.

## WIP

still need work.

* anchors will become left-top now, this is not ideal behaviour. 
* can open only 1 file, this is shortage of UI. 
* D&D-ed target does not become selected, this is not intended behaviour. finding solution.
* the display ratio of figma layout is not configurable yet.
* cannot modify layout when the original figma layout contains overlayed contents. this is very stressfull and we'll be add "remove unnecessary layer" feature soon.