(() => {
    const root = document.querySelector('.tempest');
    const get = name => document.getElementById(`tempest-${name}`);
    const names = ['Sapphire', 'Emerald', 'Ruby', 'Amethyst', 'Star', 'Crystal', 'Castle', 'Crown', 'Sun', 'Scatter', 'Lightning'];
    // Standalone vector artwork stays crisp at every reel size and has no background.
    const artwork = (body, light = '#fff0a6', mid = '#e6aa32', dark = '#865014') =>
        'data:image/svg+xml,' + encodeURIComponent(`<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 100 100"><defs><linearGradient id="g" x2=".8" y2="1"><stop stop-color="${light}"/><stop offset=".25" stop-color="${mid}"/><stop offset=".42" stop-color="${light}"/><stop offset=".55" stop-color="${mid}"/><stop offset="1" stop-color="${dark}"/></linearGradient><radialGradient id="heart" cx=".3" cy=".2" r=".85"><stop stop-color="#fff"/><stop offset=".18" stop-color="${light}"/><stop offset=".5" stop-color="${mid}"/><stop offset="1" stop-color="${dark}"/></radialGradient><linearGradient id="edge" x2="0" y2="1"><stop stop-color="#fff7cb"/><stop offset="1" stop-color="#b27825"/></linearGradient></defs>${body}<path d="M25 12l2 5 5 2-5 2-2 5-2-5-5-2 5-2z" fill="#fff" opacity=".85"/></svg>`);
    const gem = (outline, center, light, mid, dark) => artwork(
        `<path d="${outline}" fill="url(#g)" stroke="#eacb83" stroke-width="4" stroke-linejoin="round"/><path d="${center}" fill="url(#heart)" stroke="${light}" stroke-width="1.5"/><path d="M50 10L50 30 20 40z" fill="#fff" opacity=".4"/><path d="M20 40L50 88 50 67z" fill="${dark}" opacity=".6"/><path d="M50 30L80 40 50 67z" fill="#fff" opacity=".25"/><path d="M28 40L50 28 72 40M28 40L50 69 72 40" fill="none" stroke="#fff" stroke-width=".8" opacity=".7"/><path d="M35 34L37 41 44 43 37 45 35 52 33 45 26 43 33 41Z" fill="#fff" opacity=".9"/>`, light, mid, dark);
    const symbols = [
        gem('M50 9L87 39 50 91 13 39Z', 'M50 28L72 40 50 69 28 40Z', '#b0efff', '#299de9', '#16438f'),
        gem('M29 12H71L86 29V70L70 88H30L14 70V29Z', 'M32 29H68V70H32Z', '#c2ffda', '#23cc87', '#086346'),
        gem('M50 10L90 80Q50 98 10 80Z', 'M50 32L72 73H28Z', '#ffd2d9', '#f4446a', '#930b43'),
        gem('M50 8L83 30 86 68 50 92 14 68 17 30Z', 'M50 29L69 41 68 62 50 73 32 62 31 41Z', '#f2d9ff', '#ae62f0', '#542898'),
        artwork('<path d="M50 9L61 35 90 38 68 57 75 86 50 71 25 86 32 57 10 38 39 35Z" fill="url(#g)" stroke="#fff1c6" stroke-width="2"/><path d="M50 9V52L10 38 39 35Z" fill="#fff6d2" opacity=".6"/><path d="M50 52L75 86 50 71 25 86Z" fill="#ad6019"/><circle cx="50" cy="51" r="10" fill="#cb79f7" stroke="#fff5b8" stroke-width="3"/>'),
        gem('M50 5L75 26 83 66 50 95 17 66 25 26Z', 'M50 25L63 37 67 61 50 77 33 61 37 37Z', '#d6ffff', '#46dbe5', '#15718d'),
        artwork('<path d="M18 85V40H34V85M66 85V40H82V85" fill="url(#g)" stroke="#ffecc0" stroke-width="2"/><path d="M12 41L26 18 40 41M60 41L74 18 88 41" fill="#6297e7" stroke="#c9e4ff" stroke-width="2"/><path d="M33 85V50H41V23H59V50H67V85Z" fill="url(#g)" stroke="#ffecc0" stroke-width="2"/><path d="M36 25L50 7 64 25Z" fill="#779be9" stroke="#d9e9ff" stroke-width="2"/><path d="M43 85V69Q50 58 57 69V85" fill="#30264b" stroke="#ffdf89" stroke-width="2"/><path d="M23 49H29V59H23ZM71 49H77V59H71ZM47 33H53V44H47Z" fill="#293958"/><path d="M14 87H86" stroke="url(#edge)" stroke-width="6"/>', '#fff1c5', '#cfb88b', '#74678e'),
        artwork('<path d="M15 29L32 48 50 16 68 48 85 29 77 77H23Z" fill="url(#g)" stroke="#fff0b5" stroke-width="2.5"/><path d="M25 68H75V83H25Z" fill="url(#g)" stroke="#fff0b5" stroke-width="2"/><path d="M50 35L59 51 50 65 41 51Z" fill="#e33267" stroke="#ffeaba" stroke-width="2"/><circle cx="30" cy="58" r="4" fill="#45cbe9"/><circle cx="70" cy="58" r="4" fill="#45cbe9"/><g fill="#fff0b5"><circle cx="15" cy="27" r="5"/><circle cx="50" cy="14" r="5"/><circle cx="85" cy="27" r="5"/></g>'),
        artwork('<path d="M50 4L58 22 73 10 74 30 94 28 81 45 97 56 77 62 84 82 64 77 60 97 48 82 33 94 31 74 11 79 20 59 3 49 23 41 14 22 35 27Z" fill="url(#g)" stroke="#ffdf90" stroke-width="1.5"/><circle cx="50" cy="51" r="25" fill="url(#g)" stroke="#fff4bc" stroke-width="3"/><path d="M50 31L56 44 70 46 59 56 62 70 50 63 38 70 41 56 30 46 44 44Z" fill="#fff0a9"/>'),
        artwork('<path d="M18 85V43Q18 9 50 9Q82 9 82 43V85H68V43Q68 24 50 24Q32 24 32 43V85Z" fill="url(#g)" stroke="#fff2be" stroke-width="2"/><path d="M35 83V45Q35 28 50 28Q65 28 65 45V83Z" fill="#703dcc" opacity=".8"/><path d="M53 33L39 57H49L44 75 63 48H52Z" fill="#f8dcff"/><path d="M13 86H87" stroke="#fbd987" stroke-width="6"/><text x="50" y="95" text-anchor="middle" fill="#fff4ce" font-family="sans-serif" font-weight="bold" font-size="10">SCATTER</text>'),
        artwork('<path d="M57 5L17 57H43L34 95 86 37H58L70 5Z" fill="url(#g)" stroke="#fff5c1" stroke-width="3" stroke-linejoin="round"/><path d="M57 12L27 52H49L40 78 71 43H52Z" fill="#fff7b5" opacity=".7"/>')
    ];
    symbols[6] = artwork('<path d="M12 87V48H29V34H40V20H60V34H71V48H88V87Z" fill="url(#g)" stroke="#ffe7a0" stroke-width="2"/><path d="M7 49L21 25 35 49M31 35L50 6 69 35M65 49L79 25 93 49" fill="url(#heart)" stroke="#a0ffff" stroke-width="2"/><path d="M42 87V64Q50 49 58 64V87" fill="#05304c" stroke="#ffe9aa" stroke-width="3"/><path d="M18 57V68M80 57V68M48 39V48M32 53V61M67 53V61" stroke="#a8ffff" stroke-width="4"/><path d="M8 87H92M13 79H38M63 79H87M15 73H35M65 73H86" stroke="#eac484" stroke-width="3"/><path d="M10 86Q2 70 12 61M88 86Q99 68 89 62" fill="none" stroke="#47d6c5" stroke-width="3"/>', '#fff2cc', '#bca477', '#4d6170');
    symbols[7] = artwork('<path d="M10 27L30 46 50 12 70 46 90 27 79 80H21Z" fill="url(#g)" stroke="#fff1b8" stroke-width="3"/><path d="M20 32L29 63 40 51 50 23 60 51 71 63 80 32" fill="none" stroke="#8a5118" stroke-width="2"/><path d="M22 69Q50 77 78 69L77 86Q50 93 23 86Z" fill="url(#g)" stroke="#fff3c4" stroke-width="2"/><path d="M50 34L63 51 50 69 37 51Z" fill="url(#heart)" stroke="#fff0b5" stroke-width="3"/><g fill="#2ff3de" stroke="#fff3bd" stroke-width="2"><circle cx="29" cy="57" r="5"/><circle cx="71" cy="57" r="5"/><circle cx="35" cy="81" r="3"/><circle cx="65" cy="81" r="3"/></g><g fill="#fff3d1"><circle cx="10" cy="25" r="5"/><circle cx="50" cy="10" r="5"/><circle cx="90" cy="25" r="5"/></g>', '#fff4b5', '#e9b444', '#925b20');
    const draw = (board, winning = []) => {
        get('board').replaceChildren(...board.map((symbol, i) => {
            const cell = document.createElement('div');
            cell.className = `tempest-symbol symbol-${symbol}${winning.includes(i) ? ' winning' : ''}`;
            const icon = document.createElement('img');
            icon.src = symbols[symbol]; icon.alt = ''; icon.draggable = false;
            cell.append(icon); cell.title = names[symbol];
            cell.setAttribute('aria-label', names[symbol]);
            return cell;
        }));
    };
    draw(Array.from({ length: 30 }, (_, i) => (i * 7 + Math.floor(i / 6)) % 11));
    const pause = () => new Promise(resolve => setTimeout(resolve, get('fast').checked ? 60 : 650));
    const reducedMotion = window.matchMedia('(prefers-reduced-motion: reduce)');
    const animate = async (cell, frames, duration, delay = 0) => {
        const speed = get('fast').checked ? 0.25 : 1;
        const animation = cell.animate(frames, {
            duration: reducedMotion.matches ? 1 : duration * speed,
            delay: reducedMotion.matches ? 0 : delay * speed,
            easing: 'cubic-bezier(.35,0,.7,1)', fill: 'both'
        });
        await animation.finished;
        // Keep the final appearance until the next board replaces these cells.
        animation.commitStyles();
        animation.cancel();
    };
    const clearBoard = async () => {
        window.TempestAudio?.effect('spin');
        const board = get('board');
        await Promise.all([...board.children].map((cell, i) => {
            cell.classList.remove('winning');
            return animate(cell, [
                { transform: 'translateY(0)', opacity: 1 },
                { transform: `translateY(${board.clientHeight + cell.clientHeight}px)`, opacity: 0 }
            ], 420, (4 - Math.floor(i / 6)) * 35 + (i % 6) * 25);
        }));
    };
    const dropBoard = async (board, previous) => {
        draw(board);
        const cells = [...get('board').children];
        const pitch = cells[6].offsetTop - cells[0].offsetTop;
        const removed = new Set(previous?.winning || []);
        const falls = [];
        for (let col = 0; col < 6; col++) {
            const survivors = previous
                ? Array.from({ length: 5 }, (_, row) => row).filter(row => !removed.has(row * 6 + col))
                : [];
            const added = 5 - survivors.length;
            for (let row = 0; row < 5; row++) {
                const cell = cells[row * 6 + col];
                // Survivors fall only as far as the holes below them. New symbols enter above the board.
                const sourceRow = row < added ? row - added : survivors[row - added];
                const distance = (sourceRow - row) * pitch;
                if (!distance) continue;
                falls.push(animate(cell, [
                    { transform: `translateY(${distance}px)`, opacity: 1 },
                    { transform: 'translateY(3px)', opacity: 1, offset: 0.88 },
                    { transform: 'translateY(0)', opacity: 1 }
                ], 520, col * 35));
            }
        }
        await Promise.all(falls);
        window.TempestAudio?.effect('drop');
    };
    const bubbleLayer = document.createElement('div');
    bubbleLayer.className = 'tempest-bubbles';
    bubbleLayer.setAttribute('aria-hidden', 'true');
    root.append(bubbleLayer);
    const releaseBubbles = cell => {
        if (reducedMotion.matches) return;
        const origin = cell.getBoundingClientRect(), bounds = root.getBoundingClientRect();
        for (let i = 0; i < 7; i++) {
            const bubble = document.createElement('span');
            const size = 5 + Math.random() * 14;
            bubble.style.cssText = `width:${size}px;height:${size}px;left:${origin.left - bounds.left + origin.width * (.2 + Math.random() * .6)}px;top:${origin.top - bounds.top + origin.height * (.35 + Math.random() * .4)}px`;
            bubbleLayer.append(bubble);
            const drift = (Math.random() - .5) * 70;
            const rise = origin.height * (1.2 + Math.random() * 1.6);
            const motion = bubble.animate([
                { transform: 'translate(0,0) scale(.3)', opacity: 0 },
                { transform: `translate(${drift * .2}px,${-rise * .15}px) scale(1)`, opacity: .95, offset: .2 },
                { transform: `translate(${drift}px,${-rise}px) scale(.7)`, opacity: 0 }
            ], { duration: (800 + Math.random() * 500) * (get('fast').checked ? .45 : 1), delay: i * 18, fill: 'both', easing: 'ease-out' });
            motion.finished.then(() => bubble.remove(), () => bubble.remove());
        }
    };
    const removeWinners = async winning => {
        window.TempestAudio?.effect('pop');
        await Promise.all(winning.map(index => {
            const cell = get('board').children[index];
            releaseBubbles(cell);
            cell.classList.remove('winning');
            return animate(cell, [
                { transform: 'scale(1)', opacity: 1, filter: 'brightness(1)' },
                { transform: 'scale(1.16)', opacity: 1, filter: 'brightness(2)', offset: 0.3 },
                { transform: 'scale(0)', opacity: 0, filter: 'brightness(2)' }
            ], 320);
        }));
    };
    let busy = false;
    get('form').addEventListener('submit', async event => {
        event.preventDefault();
        if (busy || !get('form').reportValidity()) return;
        window.TempestAudio?.start();
        busy = true; get('spin').disabled = true; get('bet').disabled = true;
        get('win').textContent = '0'; get('multiplier').textContent = '×1';
        get('status').textContent = 'Calling the storm…';
        try {
            await clearBoard();
            const body = new URLSearchParams({ bet: get('bet').value,
                __RequestVerificationToken: root.querySelector('[name="__RequestVerificationToken"]').value });
            const response = await fetch(root.dataset.endpoint, { method: 'POST', body });
            if (response.redirected) throw new Error('Your session expired. Refresh and sign in again.');
            const data = await response.json();
            if (!response.ok) throw new Error(data.message || 'Spin failed.');
            get('balance').textContent = data.balance.toLocaleString();
            let revealed = 0;
            for (const [index, turn] of data.result.turns.entries()) {
                if (index > 0) await clearBoard();
                get('free').textContent = turn.freeSpin ? `${index}/${data.result.turns.length - 1}` : '0';
                get('multiplier').textContent = `×${turn.multiplier}`;
                for (const [frameIndex, frame] of turn.frames.entries()) {
                    const previous = turn.frames[frameIndex - 1];
                    if (previous) await removeWinners(previous.winning);
                    await dropBoard(frame.board, previous);
                    if (frame.win > 0) window.TempestAudio?.effect('win');
                    frame.winning.forEach(i => get('board').children[i].classList.add('winning'));
                    get('status').textContent = frame.win > 0 ? `Tumble win: ${frame.win.toLocaleString()} credits` : turn.freeSpin ? 'Free spin' : 'The storm settles';
                    await pause();
                }
                revealed += turn.win; get('win').textContent = revealed.toLocaleString();
                if (turn.awarded) { window.TempestAudio?.effect('bonus'); get('status').textContent = `${turn.awarded} free spins awarded!`; await pause(); }
            }
            get('free').textContent = '0';
            get('status').textContent = data.result.capped ? 'Maximum win! 5,000× bet.' : revealed ? `Round complete · ${revealed.toLocaleString()} credits won` : 'Round complete · Try another spin';
        } catch (error) {
            get('status').textContent = `${error.message} Refresh to confirm your balance before spinning again.`;
            // A lost response can still represent a settled round; require a refresh.
            get('spin').textContent = 'REFRESH TO CONTINUE';
            return;
        } finally { get('bet').disabled = false; }
        busy = false; get('spin').disabled = false;
    });
})();


