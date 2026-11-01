// Continuous, bounded wandering. No timers, geometry changes or per-frame allocations.
export class WanderingMotion {
 constructor(random=Math.random){this.phase=random()*100;this.a=random()*6.28318530718;this.b=random()*6.28318530718;this.speed=1;this.boost=0;this.bassLevel=0;this.pathTime=0;this.from={x:0,y:0};this.to={x:0,y:0};this.point={x:.5,y:.5};}
 start(x=.5,y=.5){this.point.x=Math.max(0,Math.min(1,x));this.point.y=Math.max(0,Math.min(1,y));}
 beat(){this.boost=Math.min(7,this.boost+6);}
 bass(level){this.bassLevel=Number.isFinite(level)?Math.max(0,Math.min(1,level)):0;}
 step(dt){
  dt=Math.max(0,Math.min(.05,dt));
  this.boost*=Math.exp(-dt/.9);
  this.speed+=(1+Math.max(this.boost,this.bassLevel*7)-this.speed)*(1-Math.exp(-dt/.055));
  this.phase+=dt*.75*this.speed;
  const t=this.phase;
  const angle=t+.12*Math.sin(t*.47+this.a)+.06*Math.sin(t*.93+this.b);
  const radius=(2/1.5)*(.33+.035*Math.sin(t*.61+this.a)+.018*Math.cos(t*1.23+this.b));
  this.pathTime+=dt;
  const section=this.pathTime/9,mode=Math.floor(section)%5;
  let blend=Math.max(0,Math.min(1,((section%1)-2/3)*3));blend=blend*blend*(3-2*blend);
  this.path(mode,angle,this.from);this.path((mode+1)%5,angle,this.to);
  const x=.5+radius*(this.from.x+(this.to.x-this.from.x)*blend),y=.5+radius*(this.from.y+(this.to.y-this.from.y)*blend);
  const ease=1-Math.exp(-dt*8);
  this.point.x+=(x-this.point.x)*ease;this.point.y+=(y-this.point.y)*ease;
  return this.point;
 }
 path(mode,angle,out){
  if(mode===0){out.x=Math.cos(angle);out.y=Math.sin(angle);return;}
  const x=Math.sin(angle),y=.55*Math.sin(2*angle);
  if(mode===1){out.x=x;out.y=y;}
  else if(mode===2){out.x=y;out.y=x;}
  else if(mode===3){out.x=(x-y)*Math.SQRT1_2;out.y=(x+y)*Math.SQRT1_2;}
  else{out.x=(x+y)*Math.SQRT1_2;out.y=(y-x)*Math.SQRT1_2;}
 }
}
