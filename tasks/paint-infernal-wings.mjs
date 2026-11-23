// Rasterise a quadratic stroke with a variable cross section. Coordinates use the
// same shoulder-relative design space as the other wing masks.
const clamp = value => Math.max(0, Math.min(1, value));
const mix = (a, b, t) => a.map((v, i) => v + (b[i] - v) * t);

function stroke(ink, start, bend, end, radius, shade, sway) {
    const steps = Math.ceil(2.5 * (Math.hypot(bend[0]-start[0], bend[1]-start[1]) +
        Math.hypot(end[0]-bend[0], end[1]-bend[1])));
    for (let i = 0; i <= steps; i++) {
        const t = i / steps, v = 1 - t;
        const dx = v*(bend[0]-start[0]) + t*(end[0]-bend[0]);
        const dy = v*(bend[1]-start[1]) + t*(end[1]-bend[1]);
        const length = Math.hypot(dx,dy) || 1, nx = -dy/length, ny = dx/length;
        const displacement = sway ? sway(t) : 0;
        const x = v*v*start[0] + 2*t*v*bend[0] + t*t*end[0] + nx*displacement;
        const y = v*v*start[1] + 2*t*v*bend[1] + t*t*end[1] + ny*displacement;
        const r = Math.max(.3, radius(t));
        for (let s = -r; s <= r; s += .55) {
            ink(Math.round(x+nx*s), Math.round(y+ny*s), shade(s/r,t),
                .88 * clamp(r-Math.abs(s)+.25));
        }
    }
}

export function paintFelFire(ink, phase) {
    const angle = phase * 2*Math.PI;
    function tongue(start, bend, end, width, seed) {
        const sway = t => Math.sin(Math.PI*t*.85)*t*5*Math.sin(angle + t*7 + seed);
        stroke(ink, start,bend,end,
            t => width*(.25 + 1.12*Math.sin(Math.PI*t)**.8)*(1-.55*t)*(1-t**3),
            (u,t) => {
                const swirl = Math.sin(8*t-angle+seed);
                const seam = Math.abs(u - .27*swirl);
                const core = Math.exp(-6*seam*seam);
                const flare = clamp((.35+.65*core)*(.8+.20*Math.sin(10*t+seed-angle)));
                const body = mix([8,39,39],[51,191,38],flare);
                return mix(body,[224,254,82],core**4*(.32+.68*Math.sin(Math.PI*t)));
            }, sway);
    }
    // Four trailing plumes share a shoulder root and sweep outwards below the fire front.
    const plumes = [
        [[3,44],[12,80],[45,109],11],
        [[3,43],[25,74],[72,82],13],
        [[4,42],[41,63],[96,55],14],
        [[4,43],[45,44],[107,18],13]
    ];
    for (const [i,[root,bend,tip,width]] of plumes.entries()) {
        tongue(root,bend,tip,width,1.6*i);
    }

    // A continuous flame bed follows the crest. Rising and curling wisps share its
    // fuel line, so the fire still reads as one swept surface between the brighter tips.
    for (let x=2;x<109;x++) {
        const span=(x-2)/107;
        const bed=48-27*span-12*Math.sin(Math.PI*span);
        const envelope=Math.sin(Math.PI*span)**.50;
        const skirt=(8+5*Math.sin(.17*x+angle))*envelope;
        for (let y=1;y<=Math.ceil(bed+skirt);y++) {
            const rise=bed-y;
            const flow=x-.85*rise+4*Math.sin(.15*rise+angle+.07*x);
            const crests=.5+.5*Math.sin(.27*flow-angle+1.45*Math.sin(.091*flow+angle));
            const height=2+envelope*(4+13*crests+9*Math.sin(.13*flow+angle)**2);
            const above=rise/height;
            const coverage=clamp((height-rise)*.40)*clamp((rise+skirt)/Math.max(1,skirt))*
                clamp(envelope*3)*clamp(y/4);
            if (coverage<=0) continue;
            const filament=.5+.5*Math.sin(.56*flow+angle-.10*rise);
            const heat=clamp(.68-.55*Math.abs(above-.18)+.48*(filament-.5)+.24*crests);
            const green=mix([10,63,45],[68,216,30],heat);
            ink(x,y,mix(green,[228,255,94],heat**5),coverage);
        }
    }
}

export function paintDemonicBone(ink) {
    const umber=[54,26,30], ivory=[231,207,161], marrow=[192,55,48];
    function bone(a,b,c,width,pointed=false) {
        stroke(ink,a,b,c,t => {
            const knuckles = Math.exp(-((t-.08)**2)/.014) + Math.exp(-((t-.92)**2)/.012);
            return width*(.70+.44*knuckles)*(pointed ? (1-t)**.65 : 1);
        },(u,t) => {
            const ridge = Math.exp(-((u+.34)**2)/.12);
            const pores = .06*Math.sin(31*t+3*u)*Math.sin(7*u-21*t);
            const face = clamp(.72-.67*Math.abs(u+.22)+.27*ridge+pores);
            const fissure = .54*Math.exp(-((u-.28-.12*Math.sin(12*t))**2)/.014);
            return mix(umber,ivory,clamp(face-fissure));
        });
    }
    function socket([cx,cy],r) {
        for(let y=-r;y<=r;y++) for(let x=-r;x<=r;x++) {
            const d=Math.hypot(x/r,y/r);
            if(d>1) continue;
            let colour=mix(umber,ivory,clamp(.71-.39*(x+y)/r-.26*d*d));
            const cleft=Math.abs(x+.35*y+.7);
            colour=mix(colour,marrow,.9*Math.exp(-cleft*cleft/1.6));
            ink(cx+x,cy+y,colour,clamp((1-d)*r+.35));
        }
    }
    const shoulder=[5,46], elbow=[25,20], wrist=[58,17];
    const fingers=[
        [[72,14],[89,19],[106,38],[100,45]],
        [[64,26],[79,41],[94,62],[89,72]],
        [[53,32],[61,52],[78,86],[72,97]],
        [[40,33],[45,63],[53,102],[44,116]],
        [[29,38],[28,67],[30,110],[22,125]]
    ];
    for(const [i,[base,joint,tip,hook]] of fingers.entries()) {
        const knuckle=i<3?wrist:elbow;
        bone(knuckle,base,joint,4.8);
        bone(joint,[tip[0]-1,joint[1]+10],tip,3.8);
        bone(tip,[tip[0]+2,tip[1]+5],hook,3.1,true);
        socket(joint,4);
    }
    bone(shoulder,[10,30],elbow,6.5);
    bone(elbow,[42,12],wrist,5.6);
    bone(wrist,[74,3],[91,6],4.2,true);
    bone(elbow,[24,9],[33,2],4,true);
    socket(elbow,6);
    socket(wrist,5);
}
