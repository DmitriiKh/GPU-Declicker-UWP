# GPU-Declicker-UWP
The app automatically finds and repairs damaged samples (clicks, pops, bit rots) in audio files. 
![Alt text](/2020-06-24.png?raw=true "GPU-Declicker-UWP")

<b>Basic Using</b>

Open an audio file that needs repair.

Scan it. If results are not okay change "Detection level" or/and "Max length" settings. Lower value of "Detection level" will increase sensitivity of detection algorithm. "Max length" value determs maximum length of restoration. For fixing bit rots I'd recomend 10 on the "Max length" slider.

Save results.

<b>Advanced Using</b>

The app gives user full control on changing results of automatical detection and repair. User can see a list of clicks in graphical form. Also user can change start position and length of a click or exclude clicks from the list.

<b>AudioGraph API</b>

AudioGraph API used for reading and writing audio files. 

<b>Known Issuses</b>

Lengths of input and output files are different by about 1000 samples.

<b>This project was created with support from JetBrains</b>
![JetBrains](/jetbrains.png?raw=true "")
https://www.jetbrains.com/?from=GPU-Declicker-UWP
