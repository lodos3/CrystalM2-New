import { Resvg } from '@resvg/resvg-js';
import { gzipSync } from 'node:zlib';
import { writeFileSync } from 'node:fs';

// One independently compressed GPU page per style. Art is drawn in shoulder-relative pixels.
const width=164, height=188, pad=26, pageSize=1024, frames=6;
const palettes=[
 ['201525','753629','e65b24','ffdc82'],['102948','16667b','42c8ba','e0ffef'],
 ['251b39','52376b','75a5a9','ffe3ab'],['22394b','527f85','b1e4d7','fffae0'],
 ['192e32','276348','a4c355','ffe7a1'],['181d3c','414473','7e8ee4','f3ecff'],
 ['251334','51335a','82b934','e8fb72'],['55263b','9e4727','efa644','fff3b5'],
 ['211732','4b285a','a161b3','ffe5db'],['1d2344','314c7a','74b6d6','ffe7b4']
];
const path=(d,fill,stroke='none',w=1)=>`<path d="${d}" fill="${fill}" stroke="${stroke}" stroke-width="${w}" stroke-linejoin="round" stroke-linecap="round"/>`;
const stroke=(d,c,w=1)=>path(d,'none',c,w);
const grad='url(#v)', metal='url(#m)';
function design(id,time){
 const [shadow,body,rim,hot]=palettes[id].map(v=>'#'+v);
 const beat=Math.sin(time), shine=.55+.23*Math.cos(time);
 const jewel=(x,y,r,c=hot)=>`<circle cx="${x}" cy="${y}" r="${r}" fill="${c}"/>`;
 let s='';
 switch(id){
 case 0: { // Scalloped drake sail, lava fissures and an obsidian finger on every lobe.
  s+=path('M3 45Q15 15 52 7Q78 13 106 3Q88 22 98 42Q73 32 75 66Q51 48 47 91Q28 72 22 119Q18 73 3 45Z',grad,rim,1);
  const ribs=[[22,116,22,53],[46,90,34,47],[74,65,51,28],[98,42,67,17],[107,4,64,15]];
  for(const [x,y,b,c] of ribs){
   s+=path(`M4 44Q${b} ${c-13} ${x} ${y}Q${b-6} ${c-2} 6 48Z`,metal,shadow,1.3);
   s+=stroke(`M7 44Q${b} ${c-10} ${x-3} ${y-6}`,hot,1);
  }
  for(let i=0;i<4;i++){const x=21+i*15,y=51-i*8;
   s+=stroke(`M${x} ${y}l4 -7 -1 -3 7 -7 m-6 9 6 1 3 -4`,rim,1.3);}
  break;
 }
 case 1: { // A dorsal fin carries long translucent kelp-like streamers.
  s+=path('M3 45C13 16 38 2 65 7L52 19Q79 9 100 15L78 29Q96 26 106 33Q68 46 47 62Q23 66 3 45Z',grad,rim);
  for(let i=0;i<6;i++){let x=34+i*13,y=120-i*15, drift=4*Math.sin(time+i);
   s+=path(`M${5+i*2} 45Q${21+i*8} ${24-i*2} ${x} ${y-19}q4 16 ${11+drift} 8q-8 18 -17 2Q${14+i*8} ${52-i} 5 48Z`,grad);
   s+=stroke(`M8 46Q${24+i*8} ${27-i*2} ${x-2} ${y-16}q4 16 ${10+drift} 11`,i%2?rim:hot,1.3);}
  break;
 }
 case 2: { // A broken mineral arch and three different planes on every crystal.
  s+=path('M3 47L12 26 32 22 40 12 66 13 73 21 48 30 28 34 11 52Z',metal);
  for(const [x,y,dx,dy,r] of [[15,40,4,65,8],[25,35,15,50,11],[34,28,25,40,11],[48,22,36,22,9],[58,20,47,-9,7],[30,25,5,-20,5],[54,17,22,-13,4]]){
   const k=x+dx*.64,j=y+dy*.64;
   s+=path(`M${x} ${y}L${k-r} ${j+3} ${x+dx} ${y+dy} ${k+r} ${j-5}Z`,grad,shadow,1.5);
   s+=path(`M${x} ${y}L${k+2} ${j-4} ${x+dx} ${y+dy} ${k+r} ${j-5}Z`,metal);
   s+=stroke(`M${k-r} ${j+3}l${r+2} -7 ${r-2} -1`,hot,.9);}
  break;
 }
 case 3: { // Rolled cloud crests opening into calligraphic wind streamers.
  for(const [i,[x,y]] of [[94,15],[79,43],[57,73],[34,110]].entries()){
   const drift=3*Math.sin(i+time);
   s+=path(`M3 46C20 ${12+i*7} ${x-20} ${y+17} ${x} ${y}q8 -13 0 -14q-7 2 -4 8q-13 -6 -4 -15q18 -13 21 4q3 20 -20 30C${x-23} ${y+30+drift} 24 ${41+i*9} 4 49Z`,grad);
   s+=stroke(`M5 46C29 ${18+i*9} ${x-1} ${y+28+drift} ${x+9} ${y-6}`,hot,1.6);}
  break;
 }
 case 4: { // Monarch panes share a dark lattice and a scalloped hindwing.
  s+=path('M4 46C22 18 67 4 102 7Q110 19 94 37Q81 55 47 61Q76 57 67 78Q59 96 41 89Q38 110 25 118Q25 93 17 86Q7 71 4 46Z',grad,shadow,3);
  for(const [x,y,q,r] of [[94,13,57,16],[88,30,49,25],[72,45,40,33],[54,54,32,39],[61,73,40,60],[42,85,25,60],[28,101,18,70]]){
   s+=path(`M9 47Q${q} ${r-7} ${x} ${y}Q${x-6} ${y+11} ${q-2} ${r+12}Q20 54 9 47Z`,metal,shadow,2);}
  for(let i=0;i<9;i++)s+=jewel(97-4.9*i,16+4.4*i,1.4+(i%2)*.55);
  break;
 }
 case 5: { // Ragged raven coverts with irregular, forked lightning across their faces.
  for(let i=0;i<9;i++){
   const x=22+10*i,y=117-12*i, b=14+5*i;
   s+=path(`M3 46Q${b} ${18-i} ${x+4} ${y-26}L${x-1} ${y-19}l5 -1 -7 12 3 -1 -5 9Q${b-5} ${59-i*4} 3 48Z`,grad,shadow,1.4);}
  for(let i=0;i<3;i++){const x=26+i*22,y=29+i*2,j=3*Math.sin(time+i);
   s+=stroke(`M6 45L${x} ${y}l-2 7 ${14+j} -4 -4 9 10 -2 -1 8 5 -1 m-15 -10 4 10 -2 6`,rim,3);
   s+=stroke(`M6 45L${x} ${y}l-2 7 ${14+j} -4 -4 9 10 -2 -1 8 5 -1`,hot,1);}
  break;
 }
 case 6: { // Translucent lacewing cells and long, curved stingers.
  for(const [x,y,dx,dy] of [[4,44,100,-33],[4,45,82,8],[6,47,56,39],[7,47,25,69]]){
   s+=path(`M${x} ${y}q${dx*.43} ${dy*.5-23} ${dx} ${dy}q${-dx*.24} ${18-dy*.28} ${-dx*.62} ${13-dy*.53}Z`,grad,rim,1.4);
   for(let k=1;k<5;k++){const u=k/5; s+=stroke(`M${x+dx*u-5} ${y+dy*u-5}l5 5 7 -3 m-7 3 2 6`,shadow,1.4);}
   s+=stroke(`M${x} ${y}q${dx*.61} ${dy*.45+3} ${dx} ${dy}`,hot,1);}
  s+=path('M4 48Q16 60 17 86Q16 115 34 124Q25 119 25 104Q33 113 38 109Q23 95 25 74Q29 50 4 48Z',metal);
  break;
 }
 case 7: { // A gilded sunburst plaque: chased metal, negative centre, inset cabochon.
  s+=path('M8 84Q28 65 36 46L43 54 54 10 61 48 78 26 74 57 101 48Q87 70 68 80L57 108 47 86 30 96 34 80Z',metal,shadow,2);
  s+=path('M24 79Q50 73 54 36Q61 62 82 55Q68 72 54 81L52 95 46 80Z',grad,rim,1.3);
  s+=path('M48 72l7 -13 9 7 -8 14Z',hot);
  break;
 }
 case 8: { // Crescent of polished amethyst and three suspended fragments.
  s+=path('M84 10C33 14 21 65 43 91Q62 111 92 85C72 126 32 122 18 92C0 51 34 9 84 10Z',metal,rim,1.6);
  s+=stroke('M68 18C25 34 18 75 41 99Q56 110 71 102',hot,1.5);
  for(const [x,y,r] of [[63,45,6],[75,64,4],[57,75,2]])s+=path(`M${x} ${y-r*2}l${r} ${r*2} -${r} ${r*2} -${r} -${r*2}Z`,grad,hot,.9);
  break;
 }
 case 9: { // Openwork navigation instrument, pearlescent star set in tilted brass hoops.
  s+=`<g transform="rotate(26 55 65)"><ellipse cx="55" cy="65" rx="32" ry="47" fill="none" stroke="${rim}" stroke-width="2"/><ellipse cx="55" cy="65" rx="45" ry="17" fill="none" stroke="#d5a76c" stroke-width="3"/></g>`;
  s+=path('M55 16L61 53 87 41 67 64 95 75 62 74 52 114 48 76 24 87 40 68 18 60 47 57Z',metal,hot,1.1);
  s+=jewel(55,65,5,hot)+jewel(56,63,2,'white');
  break;
 }}
 return `<svg xmlns="http://www.w3.org/2000/svg" width="${width}" height="${height}"><defs>
 <linearGradient id="v" x2=".84" y2="1" gradientUnits="objectBoundingBox"><stop stop-color="${rim}"/><stop offset=".26" stop-color="${body}"/><stop offset=".54" stop-color="${shadow}"/><stop offset=".81" stop-color="${body}"/><stop offset="1" stop-color="${rim}"/></linearGradient>
 <linearGradient id="m" x2=".75" y2=".89"><stop stop-color="${shadow}"/><stop offset=".24" stop-color="${rim}"/><stop offset="${shine}" stop-color="${body}"/><stop offset=".84" stop-color="${hot}"/><stop offset="1" stop-color="${body}"/></linearGradient>
 <filter id="b" x="-40%" y="-40%" width="180%" height="180%"><feGaussianBlur stdDeviation="3.4"/></filter><g id="art">${s}</g></defs>
 <rect width="100%" height="100%" fill="black"/><g transform="translate(${pad} ${pad}) rotate(${beat*.95} 4 44)"><use xmlns:xlink="http://www.w3.org/1999/xlink" xlink:href="#art" opacity=".68" filter="url(#b)"/><use xmlns:xlink="http://www.w3.org/1999/xlink" xlink:href="#art"/></g></svg>`;
}
const pages=[];
for(let id=0;id<10;id++){
 const bytes=Buffer.alloc(pageSize**2);
 for(let f=0;f<frames;f++){
  const pixels=new Resvg(design(id,2*Math.PI*f/frames)).render().pixels;
  for(let y=0;y<height;y++)for(let x=0;x<width;x++)for(let c=0;c<3;c++)
   bytes[(1+y+c*(height+2))*pageSize+1+f*(width+2)+x]=pixels[4*(y*width+x)+2-c];
 }
 pages.push(gzipSync(bytes,{level:9}));
}
const header=Buffer.alloc(4*pages.length);
pages.forEach((data,i)=>header.writeUInt32LE(data.length,4*i));
writeFileSync(new URL('../../Client.Rendering.Core/Assets/ElementalWings.bin',import.meta.url),Buffer.concat([header,...pages]));
