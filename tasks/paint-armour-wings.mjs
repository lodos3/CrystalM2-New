import { paintFelFire, paintDemonicBone } from './paint-infernal-wings.mjs';
import { paintSeraph } from './paint-seraph-wings.mjs';

// Draw the wing membranes at twice the sampling resolution of a resting wing.
// The runtime bends and projects this art with the wearer instead of enlarging individual sparks.
export function paintWing(style, phaseFraction = 0) {
    const w = 112, h = 136, rgb = new Float32Array(w * h * 3);
    const mix = (a,b,t) => a.map((v,i) => v + (b[i]-v)*t);
    const clamp = v => Math.max(0, Math.min(1,v));
    function ink(x,y,col,alpha) {
        if (x < 0 || y < 0 || x >= w || y >= h) return;
        const at = 3*(y*w+x);
        for (let c=0; c<3; c++) rgb[at+c] += (col[c]-rgb[at+c])*clamp(alpha);
    }
    // A tapered curved feather. Parameter u runs across the vane, t follows its spine.
    function feather(start, middle, end, width, colours, ice, ordinal) {
        for (let k=0; k<=180; k++) {
            const t=k/180, v=1-t;
            const x=v*v*start[0]+2*v*t*middle[0]+t*t*end[0];
            const y=v*v*start[1]+2*v*t*middle[1]+t*t*end[1];
            const dx=v*(middle[0]-start[0])+t*(end[0]-middle[0]);
            const dy=v*(middle[1]-start[1])+t*(end[1]-middle[1]);
            const len=Math.hypot(dx,dy)||1;
            const breadth=width*(ice ? Math.min(t*5+.08, (1-t)*1.42) : Math.pow(Math.sin(Math.PI*t),.72))+.24;
            for (let s=-breadth; s<=breadth; s+=.55) {
                const u=s/breadth, a=Math.abs(u);
                const rib=Math.pow(.5+.5*Math.sin(t*(ice?30:88)+ordinal+7*a),5);
                const light=ice ? (u<0 ? .28+.34*v : .06+.16*rib) : .09+.36*(1-a)+.25*rib;
                let col=mix(colours[0],colours[1],light);
                col=mix(col,colours[2],clamp(.80*Math.exp(-a*11)+.60*Math.pow(a,9)+.28*v*v));
                if (style === 0) {
                    // Shadow the overlapping vane on one side of each shaft. Keep the
                    // barbs several texels apart so they survive the in-game projection.
                    const barb = .5+.5*Math.cos(2*Math.PI*(t*6.5-a*1.25)+ordinal);
                    const litFace = u<0 ? .50+.26*barb : .10+.19*barb;
                    const plumage = .84+.16*Math.sin(ordinal*2.4);
                    col = mix(colours[0], colours[1], clamp((litFace+.15*t)*plumage));
                    const shaftDistance = (s+.45)/(1+.72*v);
                    const shaft = Math.exp(-shaftDistance*shaftDistance);
                    const tip = clamp((t-.78)/.22);
                    const rim = u<0 ? Math.pow(a,6)*(.20+.42*tip) : 0;
                    col = mix(col,colours[2],clamp(.94*shaft+rim+.40*tip*tip));
                }
                const fringe = clamp((1-a)*4.5 + .12*rib);
                ink(Math.round(x-dy/len*s),Math.round(y+dx/len*s),col,.88*fringe);
            }
        }
    }

    if (style === 6 || style === 7) {
        paintSeraph(ink, phaseFraction, style === 7);
    } else if (style === 4) {
        paintFelFire(ink, phaseFraction);
    } else if (style === 5) {
        paintDemonicBone(ink);
    } else if (style === 3) {
        // Separated auroral plumes with a branching discharge running down each quill.
        // Use the same endpoints each cycle so the stars stay attached as the arcs move.
        const phase = 2*Math.PI*phaseFraction;
        function filament(a,b,width,colour) {
            const dx=b[0]-a[0], dy=b[1]-a[1], length=dx*dx+dy*dy || 1;
            for(let y=Math.floor(Math.min(a[1],b[1])-width-1);y<=Math.ceil(Math.max(a[1],b[1])+width+1);y++)
            for(let x=Math.floor(Math.min(a[0],b[0])-width-1);x<=Math.ceil(Math.max(a[0],b[0])+width+1);x++) {
                const t=clamp(((x-a[0])*dx+(y-a[1])*dy)/length);
                const d=Math.hypot(x-a[0]-t*dx,y-a[1]-t*dy);
                ink(x,y,colour,clamp(width+.55-d));
            }
        }
        const stars=[];
        for(let i=0;i<6;i++) {
            const t=i/5, end=[29+73*t,122-113*t];
            const bend=[12+45*t,27-35*t], start=[4+11*t,47-14*t];
            feather(start,bend,end,7+2*Math.sin(Math.PI*t),
                [[22,16,71],[96,48,182],[128,210,253]],false,i);
            let last=start;
            for(let k=1;k<=9;k++) {
                const u=k/9,v=1-u;
                const jitter=Math.sin(i*31+k*9.1+phase)*Math.sin(Math.PI*u);
                const next=[v*v*start[0]+2*u*v*bend[0]+u*u*end[0]+3.8*jitter,
                            v*v*start[1]+2*u*v*bend[1]+u*u*end[1]-2.2*jitter];
                filament(last,next,1.65,[67,88,213]);
                filament(last,next,.65,[183,244,255]);
                if(k===4 || k===7) {
                    const fork=[next[0]+4+3*jitter,next[1]+2-i*.6];
                    filament(next,fork,.74,[154,187,255]);
                    filament(fork,[fork[0]+1.8,fork[1]+4],.48,[105,101,213]);
                }
                if(k===6 && i%2===0 || k===9 && i%2===1) stars.push(next);
                last=next;
            }
        }
        for(const [i,[x,y]] of stars.entries()) {
            const r=2.8+1.4*Math.sin(phase+i*2);
            filament([x-r,y],[x+r,y],.65,[255,220,144]);
            filament([x,y-r*1.35],[x,y+r*1.35],.72,[255,238,191]);
        }
    } else if (style !== 2) {
        const ice = style === 1;
        const palette = ice ? [[24,61,141],[52,193,246],[209,248,255]] : [[53,10,23],[241,81,19],[255,215,112]];
        // Descending primaries fold into one another; the shortened coverts overlap their bases.
        const primaryCount=ice?12:8, covertCount=ice?9:6;
        for (let i=0; i<primaryCount; i++) {
            const t=i/(primaryCount-1), bend=Math.sin(t*Math.PI);
            feather([5+39*t,41-22*t-11*bend],[12+58*t,83-73*t-10*bend],
                [25+78*t,125-108*t-9*bend],ice?11:9.5,palette,ice,i);
        }
        for (let i=0; i<covertCount; i++) {
            const t=i/(covertCount-1);
            feather([3+25*t,43-23*t],[9+45*t,21-20*t],[21+73*t,72-62*t],
                ice?9:10,palette,ice,i+13);
        }
        feather([3,44],[26,-1],[94,12],6,palette,ice,8);
    } else {
        // Two silk membranes share a shoulder. Each angular sector has a scalloped rim and a branching vein.
        function membrane(cx,cy,rx,ry,skew,start,end,lower) {
            for (let y=1; y<h-1; y++) for (let x=1; x<w-1; x++) {
                const px=(x-cx)/rx, py=(y-cy-skew*(x-cx))/ry;
                const angle=Math.atan2(py,px), u=(angle-start)/(end-start);
                if (u<0 || u>1) continue;
                const sector=u*(lower?5:7);
                const maxR=.96+.04*Math.cos(sector*2*Math.PI);
                const r=Math.hypot(px,py)/maxR;
                if (r>1) continue;
                const vein=Math.pow(.5+.5*Math.cos(sector*2*Math.PI+.20*Math.sin(r*7)),18);
                const band=Math.exp(-Math.pow((r-.80)/.05,2));
                const rim=Math.exp(-Math.pow((r-.96)/.035,2));
                const col=mix([66,26,106],[172,81,204],.25+.36*Math.sin(r*2.9)+.23*band);
                const fringe = clamp((1-r)*12) * clamp(Math.min(u,1-u)*35);
                ink(x,y,mix(col,[255,200,248],clamp(.70*vein+.74*rim+.15*band)),fringe);
            }
        }
        membrane(6,52,57,66,.17,-.10,1.48,true);
        membrane(6,52,94,44,-.30,-1.55,.66,false);
        for (const [cx,cy,rx,ry] of [[69,32,12,7],[31,88,8,11]]) {
            for (let y=cy-ry-2; y<=cy+ry+2; y++) for(let x=cx-rx-2; x<=cx+rx+2; x++) {
                const r=Math.hypot((x-cx)/rx,(y-cy+.20*(x-cx))/ry);
                if (r>1.05) continue;
                const col = r>.82 ? [249,191,217] : r>.60 ? [83,39,112] : r>.33 ? [84,208,215] : [31,58,104];
                ink(x,y,col,clamp((1.05-r)*12));
            }
        }
    }
    // Keep empty texels beyond the feathers so the wide bloom falls to zero before the quad ends.
    const felFire=style===4;
    const pad=felFire ? 38 : 26, width=w+2*pad, height=h+2*pad;
    const padded=new Float32Array(width*height*3);
    for(let y=0;y<h;y++) padded.set(rgb.subarray(y*w*3,(y+1)*w*3),((y+pad)*width+pad)*3);
    const selectiveBloom=style===0 || style===4 || style===5 || style>=6;
    const edge=gaussian(padded,width,height,selectiveBloom ? .75 : 1.65);
    const emission=selectiveBloom?padded.slice():padded;
    if (selectiveBloom) {
        // Isolate the hotter quills, flame cores or red marrow from the shaded material.
        for(let at=0;at<emission.length;at+=3) {
            const hot=style===5 ? clamp((padded[at]-1.5*padded[at+1]-30)/76) :
                felFire ? clamp((padded[at+1]-72)/127) : clamp((padded[at+1]-58)/125);
            for(let c=0;c<3;c++) emission[at+c]*=hot;
        }
    }
    const near=gaussian(emission,width,height,felFire ? 5.1 : 4.3);
    const far=gaussian(emission,width,height,felFire ? 12.5 : 8.5);
    const bgra=Buffer.alloc(width*height*4);
    for(let i=0;i<width*height;i++) {
        for(let c=0;c<3;c++) {
            const at=i*3+c;
            const core=selectiveBloom ? .80*padded[at]+.20*edge[at] : .42*padded[at]+.58*edge[at];
            // Add coloured spill in the gaps and around the tips without washing out the quills.
            const headroom=1-.76*clamp(core/190);
            const spill=felFire ? .76*near[at]+1.48*far[at] : .52*near[at]+.78*far[at];
            bgra[i*4+2-c]=Math.min(255,core+headroom*spill);
        }
        bgra[i*4+3]=255;
    }
    return {w:width,h:height,x:-pad,y:-pad,bgra};
}

function gaussian(source,width,height,sigma) {
    const radius=Math.ceil(3*sigma);
    const kernel=Array.from({length:2*radius+1},(_,i)=>Math.exp(-.5*((i-radius)/sigma)**2));
    const sum=kernel.reduce((a,b)=>a+b);
    const weights=kernel.map(v=>v/sum);
    function pass(input,vertical) {
        const output=new Float32Array(input.length);
        for(let y=0;y<height;y++) for(let x=0;x<width;x++) {
            const at=(y*width+x)*3;
            const pos=vertical?y:x, limit=vertical?height:width, stride=vertical?width*3:3;
            for(let d=Math.max(-radius,-pos);d<=Math.min(radius,limit-1-pos);d++) {
                const from=at+d*stride, weight=weights[d+radius];
                for(let c=0;c<3;c++) output[at+c]+=input[from+c]*weight;
            }
        }
        return output;
    }
    return pass(pass(source,false),true);
}
