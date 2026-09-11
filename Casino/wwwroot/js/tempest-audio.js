(() => {
    let context, music, effects, timer, next = 0, beat = 0;
    const settings = {music:true,effects:true};
    const levels = { music: 1.5, effects: 1.1 };
    try { Object.assign(settings,JSON.parse(localStorage.getItem('tempest-audio') || '{}')); } catch {}
    const tone = (frequency,time,length,volume,bus,type='sine',end=frequency) => {
        const oscillator=context.createOscillator(), gain=context.createGain();
        oscillator.type=type;
        oscillator.frequency.setValueAtTime(frequency,time);
        oscillator.frequency.exponentialRampToValueAtTime(end,time+length);
        gain.gain.setValueAtTime(0,time);
        gain.gain.linearRampToValueAtTime(volume,time+.015);
        gain.gain.exponentialRampToValueAtTime(.0001,time+length);
        oscillator.connect(gain); gain.connect(bus);
        oscillator.start(time); oscillator.stop(time+length+.02);
        oscillator.onended=()=>{oscillator.disconnect();gain.disconnect();};
    };
    const schedule=()=>{
        if(!context || context.state!=='running' || document.hidden) return;
        if(next<context.currentTime) next=context.currentTime+.05;
        while(next<context.currentTime+.3) {
            const chords=[[146.83,174.61,220],[130.81,164.81,196],[116.54,146.83,174.61],[130.81,164.81,196]];
            const chord=chords[Math.floor(beat/16)%4];
            if(settings.music) {
                if(beat%4===0) tone(chord[0]/2,next,1.3,.1,music,'triangle');
                if(beat%16===0) chord.forEach(f=>tone(f,next,3.8,.025,music));
                tone(chord[[0,2,1,2,0,1,2,1][beat%8]]*(beat%8===6?4:2),next,.7,.045,music);
            }
            beat++;next+=.3;
        }
    };
    const start=()=>{
        try {
            if(!context) {
                const Audio=window.AudioContext||window.webkitAudioContext;
                if(!Audio) return;
                context=new Audio(); music=context.createGain();effects=context.createGain();
                music.connect(context.destination);effects.connect(context.destination);
                music.gain.value = settings.music ? levels.music : 0; effects.gain.value = settings.effects ? levels.effects : 0;
                timer=setInterval(schedule,100);
            }
            if(context.state==='suspended') context.resume().then(schedule).catch(()=>{});
        } catch {}
    };
    const effect=name=>{
        if(!context || context.state!=='running' || !settings.effects || document.hidden)return;
        const now=context.currentTime;
        if(name==='spin')tone(420,now,.4,.18,effects,'triangle',65);
        if(name==='drop')[260,330,390].forEach((f,i)=>tone(f,now+i*.035,.11,.09,effects,'triangle'));
        if(name==='pop')[880,1174,1568].forEach((f,i)=>tone(f,now+i*.025,.16,.08,effects));
        if(name==='win'||name==='bonus')(name==='bonus'?[587,740,880,1174,1480,1760]:[587,740,880]).forEach((f,i)=>tone(f,now+i*.1,.45,.1,effects));
    };
    for(const kind of ['music','effects']) {
        const button=document.getElementById(`tempest-${kind}`);
        const update=()=>{button.textContent=`${kind==='music'?'Music':'Sound'}: ${settings[kind]?'on':'off'}`;button.setAttribute('aria-pressed',String(settings[kind]));};
        update();button.addEventListener('click',()=>{
            settings[kind]=!settings[kind];start();update();
            if(context)(kind==='music'?music:effects).gain.setTargetAtTime(settings[kind] ? levels[kind] : 0,context.currentTime,.03);
            try{localStorage.setItem('tempest-audio',JSON.stringify(settings));}catch{}
        });
    }
    document.addEventListener('visibilitychange',()=>{
        if(!context)return;
        if(document.hidden)context.suspend().catch(()=>{});else start();
    });
    window.addEventListener('pagehide',()=>{clearInterval(timer);context?.close().catch(()=>{});});
    window.addEventListener('pageshow',event=>{if(event.persisted)context=null;});
    window.TempestAudio={start,effect};
})();

