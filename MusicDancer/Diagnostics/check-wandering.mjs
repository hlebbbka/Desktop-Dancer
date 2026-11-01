import {WanderingMotion} from '../Web/wandering.mjs';
const motion=new WanderingMotion(()=>.42);let old={...motion.point},maxStep=0;
for(let i=0;i<100000;i++){const p=motion.step(1/140);if(p.x<-.3||p.x>1.3||p.y<-.3||p.y>1.3)throw Error('Outside enlarged orbit bounds');maxStep=Math.max(maxStep,Math.hypot(p.x-old.x,p.y-old.y));old={...p};if(i%140===0)motion.beat();}
if(maxStep>.07)throw Error('Movement discontinuity');
const calm=new WanderingMotion(()=>.42),boost=new WanderingMotion(()=>.42);boost.beat();for(let i=0;i<140;i++){calm.step(1/140);boost.step(1/140)}if(boost.phase<=calm.phase+.5)throw Error('Bass does not accelerate');for(let i=0;i<2000;i++)boost.step(1/140);if(boost.speed>1.001)throw Error('Boost does not decay');console.log('PASS wandering: 100000 frames, bounded smooth path, bass speed boost and recovery');
// A held envelope must keep the speed up after the original hit has faded.
const held=new WanderingMotion(()=>.42);held.beat();held.bass(.45);
for(let i=0;i<140*12;i++)held.step(1/140);
if(held.speed<4||held.speed>4.2)throw Error('Sustained bass acceleration was lost');
held.beat();for(let i=0;i<14;i++)held.step(1/140);
if(held.speed<5)throw Error('A new hit cannot boost a held bass');
held.bass(0);for(let i=0;i<140*12;i++)held.step(1/140);
if(held.speed>1.001)throw Error('Speed does not recover after held bass ends');
console.log('PASS sustained bass: 12s hold, fresh hit, smooth release');
