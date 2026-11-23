// Each path is a separate streamer, anchored at the shoulder. The lower curls trail
// behind the upper fan. The two designs use different silhouettes as well as palettes.
const veil = [
    [[3,44],[17,33],[28,15],[47,9],[63,11],[81,3]],
    [[4,43],[21,34],[40,17],[62,20],[84,22],[105,10]],
    [[3,44],[22,41],[44,33],[61,31],[83,40],[107,33]],
    [[4,44],[20,45],[40,46],[57,56],[81,62],[97,55]],
    [[4,45],[18,51],[30,62],[48,80],[68,82],[78,74]],
    [[4,46],[14,56],[19,74],[32,96],[52,110],[64,102]]
];
const dawn = [
    [[3,44],[12,27],[16,12],[29,6],[42,9],[57,6]],
    [[4,44],[17,30],[28,20],[52,18],[72,9],[88,12]],
    [[4,44],[23,33],[43,30],[63,33],[87,26],[105,30]],
    [[4,45],[20,43],[42,42],[64,58],[89,55],[102,46]],
    [[4,46],[17,52],[31,69],[54,83],[74,77],[81,66]],
    [[4,46],[11,60],[16,83],[34,106],[52,115],[68,105]],
    [[4,46],[8,62],[8,89],[19,111],[36,126],[49,123]]
];

export function paintSeraph(ink, phase, golden) {
    const paths = golden ? dawn : veil;
    const clock = phase * 2 * Math.PI;
    const edge = golden ? [186,91,35] : [73,130,206];
    const pearl = golden ? [255,226,154] : [220,242,255];
    function pathPoint(points,t) {
        const position = t*(points.length-1), i = Math.min(points.length-2,Math.floor(position));
        const a=points[Math.max(0,i-1)], b=points[i], c=points[i+1], d=points[Math.min(points.length-1,i+2)];
        const f=position-i;
        return b.map((v,k) => v + .5*f*((c[k]-a[k])+f*((2*a[k]-5*v+4*c[k]-d[k])+f*(3*v-a[k]-3*c[k]+d[k]))));
    }

    for (const [index,points] of paths.entries()) {
        for (let step=0;step<=360;step++) {
            const t=step/360;
            const [x,y]=pathPoint(points,t);
            const prev=pathPoint(points,Math.max(0,t-.003)), next=pathPoint(points,Math.min(1,t+.003));
            const dx=next[0]-prev[0], dy=next[1]-prev[1], length=Math.hypot(dx,dy)||1;
            const nx=-dy/length, ny=dx/length;
            const twist=clock-t*8.5+index*.97;
            const drift=t*(3.8*Math.sin(twist)+2.1*Math.sin(clock+t*5+index));
            const fold=.5+.5*Math.sin(twist);
            const width=(1-t**2)*(.8+(index%3===0?4:3)*(.25+.75*fold))+.18;
            const tail=Math.min(1,(1-t)*9);
            for(let s=-width;s<=width;s+=.45) {
                const u=s/width;
                const ridge=Math.exp(-6*(u+.36-.32*fold)**2);
                const light=Math.min(1,.24+.63*ridge+.15*(1-Math.abs(u)));
                const colour=edge.map((v,c)=>(v+(pearl[c]-v)*light)*tail);
                ink(Math.round(x+nx*(drift+s)),Math.round(y+ny*(drift+s)),colour,
                    Math.min(1,width-Math.abs(s)+.2));
            }
            // A fine filament separates from the main strand when the ribbon twists.
            const split=drift+(1.4+4*fold)*Math.sin(Math.PI*t)*(index%2===0?1:-1);
            ink(Math.round(x+nx*split),Math.round(y+ny*split),pearl,.52*tail);
        }
    }
}
