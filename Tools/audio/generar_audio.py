#!/usr/bin/env python3
"""Genera toda la música y los efectos de Tiny Swords: La Amenaza Goblin por código (sin muestras externas).

Uso:  pip install numpy
      python3 Tools/audio/generar_audio.py            # todo
      python3 Tools/audio/generar_audio.py efectos    # solo efectos (o "musica")

Los archivos quedan en Assets/Resources/Audio/{Musica,Efectos}. El AudioManager del juego los carga por nombre.
Cada efecto puede tener variantes: espada_1, espada_2... y el juego elige una al azar.
"""
import os
import sys
import wave
import numpy as np

SR = 32000
ROOT = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", "Assets", "Resources", "Audio")
rng = np.random.default_rng(7)


# ---------------------------------------------------------------- básicos
def secs(d):
    return int(round(d * SR))


def t_(d):
    return np.arange(secs(d)) / SR


def noise(d):
    return rng.uniform(-1, 1, secs(d))


def phase(freq, d):
    """Fase acumulada: freq puede ser número o arreglo (para deslizar el tono)."""
    f = np.broadcast_to(np.asarray(freq, float), (secs(d),))
    return 2 * np.pi * np.cumsum(f) / SR


def sweep(a, b, d, curve=1.0):
    x = np.linspace(0, 1, secs(d)) ** curve
    return a * (b / a) ** x


def sine(f, d):
    return np.sin(phase(f, d))


def tri(f, d):
    return 2 / np.pi * np.arcsin(np.sin(phase(f, d)))


def pulse(f, d, duty=0.5):
    """Onda cuadrada suavizada, como las consolas de 8 bits pero sin tanto zumbido."""
    p = phase(f, d)
    return np.tanh(4 * (np.sin(p) - np.cos(np.pi * duty)))


def saw(f, d):
    p = phase(f, d)
    return sum(np.sin(k * p) / k for k in range(1, 9)) * 0.6


def fft_filter(x, lo=None, hi=None, soft=0.25):
    """Filtro por frecuencia con bordes suaves (lo = corta graves, hi = corta agudos)."""
    n = len(x)
    if n == 0:
        return x
    size = 1 << int(np.ceil(np.log2(n + 2048)))
    X = np.fft.rfft(x, size)
    f = np.fft.rfftfreq(size, 1 / SR)
    g = np.ones_like(f)
    if hi:
        g *= 1 / (1 + (f / hi) ** (2 / soft))
    if lo:
        g *= 1 - 1 / (1 + (f / lo) ** (2 / soft))
    return np.fft.irfft(X * g, size)[:n]


def env(d, a=0.005, r=0.05, decay=None, curve=1.0):
    """Envolvente: ataque, caída exponencial opcional y final suave."""
    n = secs(d)
    e = np.ones(n)
    if decay:
        e *= np.exp(-np.arange(n) / SR / decay)
    na, nr = min(n, secs(a)), min(n, secs(r))
    if na:
        e[:na] *= np.linspace(0, 1, na) ** curve
    if nr:
        e[-nr:] *= np.linspace(1, 0, nr)
    return e


def mix(*parts):
    """Suma señales; cada parte es arreglo o (arreglo, inicio_s[, volumen])."""
    norm = [(p, 0, 1) if isinstance(p, np.ndarray) else (p[0], secs(p[1]), p[2] if len(p) > 2 else 1) for p in parts]
    out = np.zeros(max(s + len(x) for x, s, _ in norm))
    for x, s, g in norm:
        out[s:s + len(x)] += x * g
    return out


def drive(x, k=2.0):
    return np.tanh(x * k) / np.tanh(k)


def reverb(x, size=0.6, wet=0.2, tail=True, bright=3500, loop=False):
    n = secs(size)
    ir = np.random.default_rng(3).uniform(-1, 1, n) * np.exp(-np.arange(n) / SR / (size / 4))
    ir = fft_filter(ir, 180, bright)
    ir /= np.sqrt((ir ** 2).sum())
    total = len(x) + n
    size2 = 1 << int(np.ceil(np.log2(total)))
    y = np.fft.irfft(np.fft.rfft(x, size2) * np.fft.rfft(ir, size2), size2)[:total]
    if loop:                      # la cola de la reverberación vuelve al inicio: bucle sin corte
        y[:n] += y[len(x):total]
        return x + y[:len(x)] * wet
    if tail:
        return np.concatenate([x, np.zeros(n)]) + y * wet
    return x + y[:len(x)] * wet


def finish(x, peak=0.85, fade=0.01):
    x = np.asarray(x, float)
    x = x - x.mean()
    n = min(len(x), secs(fade))
    if n:
        x[-n:] *= np.linspace(1, 0, n)
    k = min(len(x), 32)
    x[:k] *= np.linspace(0, 1, k)
    m = np.abs(x).max()
    return x * (peak / m) if m > 0 else x


def save(folder, name, x):
    path = os.path.join(ROOT, folder)
    os.makedirs(path, exist_ok=True)
    data = (np.clip(x, -1, 1) * 32767).astype("<i2")
    with wave.open(os.path.join(path, name + ".wav"), "wb") as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(SR)
        w.writeframes(data.tobytes())


# ---------------------------------------------------------------- efectos
def whoosh(d, lo, hi, bend=0.5):
    """Silbido de aire: ruido cuya banda sube y baja."""
    n = noise(d)
    bands = np.geomspace(lo, hi, 7)
    x = np.linspace(0, 1, secs(d))
    pos = np.where(x < bend, x / bend, 1 - (x - bend) / (1 - bend) * 0.8)
    out = np.zeros_like(n)
    for i, b in enumerate(bands):
        g = np.exp(-((pos * 6 - i) ** 2) / 1.6)
        out += fft_filter(n, b * 0.75, b * 1.35) * g
    return out * np.sin(np.pi * x) ** 1.3


def clang(d, base, decay=0.12):
    """Metal: parciales inarmónicos que se apagan."""
    out = np.zeros(secs(d))
    for r, a in [(1, 1), (2.76, .6), (5.4, .4), (8.93, .22), (1.51, .5)]:
        if base * r < SR / 2.2:
            out += a * sine(base * r, d) * env(d, .001, .01, decay / r ** .5)
    return out


def thump(d, f0, f1, decay):
    return sine(sweep(f0, f1, d, 0.5), d) * env(d, .001, .02, decay)


def boom(d, size=1.0):
    body = drive(thump(d, 120 / size, 28, .35 * size), 2.5)
    blast = fft_filter(noise(d), 40, 1400 / size) * env(d, .002, .1, .28 * size)
    crack = fft_filter(noise(d), 900, 6000) * env(d, .001, .05, .05)
    debris = np.zeros(secs(d))
    for _ in range(int(14 * size)):
        s = rng.uniform(.12, d * .75)
        p = fft_filter(noise(.05), 700, 4500) * env(.05, .001, .01, .012)
        i = secs(s)
        debris[i:i + len(p)] += p[:len(debris) - i] * rng.uniform(.15, .5) * (1 - s / d)
    return mix(body, (blast, 0, 1.2), (crack, 0, .6), (debris, 0, .7))


def voice(d, f0, f1, formants, rough=0.3, breath=0.15, wob=0.0):
    """Voz de criatura: tono áspero pasado por formantes (como la boca)."""
    f = sweep(f0, f1, d, .8)
    if wob:
        f = f * (1 + wob * np.sin(2 * np.pi * 19 * t_(d)))
    f = f * (1 + rough * .08 * np.interp(np.arange(secs(d)), np.linspace(0, secs(d), int(d * 90) + 2), rng.uniform(-1, 1, int(d * 90) + 2)))
    src = saw(f, d) + breath * noise(d)
    src *= 1 + rough * np.sign(np.sin(phase(f * .5, d))) * .5
    out = np.zeros(secs(d))
    for fr, g in formants:
        out += fft_filter(src, fr * .78, fr * 1.28) * g
    return drive(out, 2.2)


def chime(notes, step, d, wave=sine, decay=.18):
    parts = []
    for i, n in enumerate(notes):
        f = midi(n)
        x = (wave(f, d) + .35 * sine(f * 2, d) + .15 * sine(f * 3.01, d)) * env(d, .002, .03, decay)
        parts.append((x, i * step))
    return mix(*parts)


def crackle(d, rate=26):
    out = fft_filter(noise(d), 60, 420) * .10
    for _ in range(int(d * rate)):
        s = rng.uniform(0, d - .04)
        p = fft_filter(noise(.02), rng.uniform(900, 2500), 7000) * env(.02, .0005, .005, .004)
        i = secs(s)
        out[i:i + len(p)] += p * rng.uniform(.2, 1) ** 2
    return out


def efectos():
    out = {}

    def add(name, x, peak=.85, fade=.01):
        out[name] = finish(x, peak, fade)

    # --- héroe
    for i, (lo, hi, d) in enumerate([(700, 4200, .24), (850, 5000, .22), (600, 3600, .26)], 1):
        add(f"espada_{i}", mix(whoosh(d, lo, hi), (clang(.22, 2300 + 150 * i, .05), .03, .10)), .7)
    for i in (1, 2, 3):
        f = 150 + 22 * i
        add(f"impacto_{i}", mix(drive(thump(.18, f, 55, .06), 3), (fft_filter(noise(.1), 500, 3500) * env(.1, .001, .02, .025), 0, .9),
                                (clang(.16, 1250 + 120 * i, .05), 0, .22)), .8)
    add("critico", mix(drive(thump(.4, 190, 38, .13), 4), (fft_filter(noise(.22), 350, 6000) * env(.22, .001, .03, .05), 0, 1.1),
                       (clang(.45, 1750, .16), 0, .45), (chime([88, 95], .04, .3, decay=.1), .05, .35)), .95)
    add("esquivar", whoosh(.28, 350, 2600, .35), .6)
    for i, f in enumerate([175, 160], 1):
        add(f"heroe_herido_{i}", mix(voice(.26, f, f * .7, [(520, 1), (1050, .7), (2300, .25)], .5) * env(.26, .01, .1, .16),
                                     (clang(.2, 980, .06), 0, .25)), .75)
    add("heroe_muere", reverb(mix(voice(.8, 170, 70, [(480, 1), (900, .6)], .7) * env(.8, .02, .4, .5), (clang(.4, 700, .12), .55, .4),
                                  (clang(.4, 520, .14), .78, .4), (thump(.35, 110, 40, .12), .75, 1)), .7, .25), .85)

    # --- poderes
    spin = np.concatenate([whoosh(.19, 500 + 160 * k, 3200 + 500 * k) * (.6 + .13 * k) for k in range(4)])
    add("torbellino", reverb(mix(spin, (crackle(.8, 60), 0, .5), (thump(.5, 140, 45, .2), .62, .8)), .5, .2), .85)
    glug = mix(*[(sine(sweep(260 + 60 * k, 520 + 70 * k, .11), .11) * env(.11, .01, .03), k * .14) for k in range(3)])
    add("pocion", reverb(mix((glug, 0, .8), (chime([76, 79, 83, 88, 91], .075, .5, decay=.22), .45, .55)), .7, .3), .7)

    # --- goblins
    small = [(780, 1), (1700, .9), (2900, .5)]
    for i, (a, b) in enumerate([(330, 480), (400, 300), (360, 540)], 1):
        add(f"goblin_grito_{i}", mix(voice(.2, a, b, small, .8) * env(.2, .01, .04), (voice(.2, b, a * .8, small, .9) * env(.2, .01, .1), .16)), .65)
    for i, a in enumerate([520, 450, 600], 1):
        add(f"goblin_herido_{i}", voice(.17, a, a * .62, small, .7, wob=.05) * env(.17, .004, .07, .1), .6)
    for i, a in enumerate([560, 480, 640], 1):
        add(f"goblin_muere_{i}", reverb(voice(.6, a, a * .28, small, .9, .25, .09) * env(.6, .006, .25, .3), .35, .15), .75)
    add("antorcha_1", mix(whoosh(.26, 260, 1500), (crackle(.26, 70), 0, .8)), .6)
    add("antorcha_2", mix(whoosh(.3, 220, 1250), (crackle(.3, 70), 0, .8)), .6)
    twang = sine(sweep(310, 190, .16), .16) * env(.16, .001, .05, .05) + .4 * pulse(sweep(155, 120, .16), .16) * env(.16, .001, .05, .03)
    add("flecha", mix((twang, 0, .8), (whoosh(.32, 1400, 5200, .25), .03, .7)), .6)
    hiss = fft_filter(noise(.55), 2500, 9000) * env(.55, .03, .12) * (1 + .5 * np.sin(2 * np.pi * 37 * t_(.55)))
    add("mecha", mix((hiss, 0, .5), (crackle(.55, 60), 0, .6)), .55)
    add("lanzar", whoosh(.22, 300, 1500, .4), .5)
    steps_ = mix(*[(thump(.12, 130, 50, .05) + .3 * clang(.12, 640 + 60 * (k % 2), .04), k * .12) for k in range(4)])
    add("embestida", mix((steps_, 0, .9), (voice(.45, 130, 170, [(450, 1), (850, .7)], .9) * env(.45, .04, .15), .05, .7)), .8)

    # --- escenario
    for i in (1, 2, 3):
        add(f"explosion_{i}", reverb(boom(1.1, 1 + .1 * i), .6, .18), .95, .1)
    add("barril", reverb(mix(boom(1.3, 1.35), (drive(fft_filter(noise(.16), 250, 2400) * env(.16, .001, .03, .05), 3), 0, .8)), .7, .2), 1.0, .1)
    add("fuego", mix(whoosh(.55, 120, 900, .25), (crackle(.6, 60), 0, .8)), .6, .08)
    wood = np.zeros(secs(1.5))
    for _ in range(16):
        s = rng.uniform(0, 1.0)
        p = drive(fft_filter(noise(.12), rng.uniform(150, 420), rng.uniform(1200, 2600)) * env(.12, .001, .03, .03), 2)
        i = secs(s)
        wood[i:i + len(p)] += p[:len(wood) - i] * rng.uniform(.3, 1) * (1.2 - s)
    add("casa_destruida", reverb(mix((wood, 0, 1), (thump(.8, 90, 30, .3), .1, .9), (fft_filter(noise(1.5), 80, 700) * env(1.5, .05, .6, .5), 0, .7)), .5, .15), .9, .15)
    for i in (1, 2):
        add(f"madera_{i}", mix(drive(thump(.14, 240 + 40 * i, 110, .04), 3), (fft_filter(noise(.09), 300, 2200) * env(.09, .001, .02, .02), 0, .9)), .7)
    stab = sum(saw(midi(n), .9) for n in (38, 44, 50, 53)) * env(.9, .004, .5, .35)
    add("emboscada", reverb(mix((fft_filter(stab, 60, 2600), 0, .5), (drive(thump(.6, 110, 35, .2), 3), 0, 1), (drive(thump(.6, 110, 35, .2), 3), .22, .8)), 1.0, .3), .9, .1)

    # --- jefe
    big = [(260, 1), (520, .9), (980, .5)]
    add("jefe_rugido", reverb(mix(voice(1.5, 62, 44, big, 1, .5, .04) * env(1.5, .12, .6), (voice(1.5, 93, 60, big, 1, .4) * env(1.5, .1, .7), 0, .5),
                                  (fft_filter(noise(1.5), 60, 350) * env(1.5, .1, .6), 0, .5)), 1.2, .35), 1.0, .2)
    add("jefe_golpe", reverb(mix(drive(thump(1.0, 95, 22, .4), 4), (fft_filter(noise(1.0), 30, 500) * env(1, .002, .4, .3), 0, 1.2),
                                 (boom(.9, .8), 0, .4)), .7, .2), 1.0, .1)
    rise = sum(saw(sweep(f, f * 1.5, 1.1), 1.1) for f in (55, 58.3, 82.4)) * env(1.1, .5, .2)
    add("jefe_invoca", reverb(mix((fft_filter(rise, 50, 1200), 0, .5), (whoosh(1.1, 200, 2200, .8), 0, .6), (thump(.6, 80, 28, .25), 1.0, 1)), 1.0, .35), .9, .1)
    add("jefe_furia", reverb(voice(1.1, 78, 110, big, 1, .5, .06) * env(1.1, .05, .4), 1.0, .35), 1.0, .15)
    add("jefe_muere", reverb(mix(voice(2.4, 75, 26, big, 1, .5, .06) * env(2.4, .05, 1.2), (boom(1.6, 1.6), 1.5, .9)), 1.4, .35), 1.0, .3)

    # --- recolectables e interfaz
    for i, n in enumerate((83, 85), 1):
        add(f"moneda_{i}", chime([n, n + 5], .06, .28, lambda f, d: .6 * pulse(f, d, .3) + .4 * sine(f, d), .1), .5)
    add("carne", mix(sine(sweep(180, 420, .1), .1) * env(.1, .004, .03), (chime([72, 76, 79], .07, .3, tri, .14), .07, .8)), .6)
    add("combo", chime([76, 81, 88], .055, .22, lambda f, d: pulse(f, d, .25) * .5 + tri(f, d) * .5, .09), .5)
    add("boton", mix(drive(thump(.07, 420, 190, .02), 2), (fft_filter(noise(.03), 900, 4000) * env(.03, .0005, .01, .006), 0, .5)), .55)
    add("boton_encima", tri(midi(79), .05) * env(.05, .003, .03, .02), .22)
    add("comprar", mix(*[(chime([84 + (k * 5) % 9, 91 + (k * 3) % 5], .03, .2, decay=.07), k * .07, .7) for k in range(5)], (chime([72, 76, 79, 84], .06, .4, tri, .2), .35, .6)), .6)
    add("pausa", chime([67, 62], .09, .22, tri, .1), .45)
    add("poder_listo", chime([79, 86], .06, .25, tri, .1), .35)

    for name, x in out.items():
        save("Efectos", name, x)
    print(f"{len(out)} efectos")


# ---------------------------------------------------------------- música
def midi(n):
    return 440.0 * 2 ** ((n - 69) / 12)


def pluck(n, d, bright=.5):
    """Laúd: cuerda pulsada (armónicos que se apagan más rápido cuanto más agudos)."""
    f, out = midi(n), np.zeros(secs(d))
    for k in range(1, 11):
        if f * k < SR / 2.3:
            out += np.sin(np.pi * k * .28) / k * sine(f * k * (1 + .0004 * k * k), d) * env(d, .002, .03, .55 / (1 + k * (1 - bright) * .7))
    return out * 1.1


def strings(n, d, hi=1800):
    f = midi(n)
    x = saw(f * .997, d) + saw(f * 1.003, d) + saw(f, d) * .6
    return fft_filter(x, 70, hi) * env(d, min(.18, d * .3), min(.25, d * .35)) * .35


def stac(n, d):
    return fft_filter(saw(midi(n), d) + saw(midi(n) * 1.004, d), 90, 2600) * env(d, .006, .04, .12) * .5


def choir(n, d):
    f = midi(n) * (1 + .006 * np.sin(2 * np.pi * 5 * t_(d)))
    src = saw(f * .996, d) + saw(f * 1.004, d) + .05 * noise(d)
    x = fft_filter(src, 420, 820) + .6 * fft_filter(src, 900, 1300)
    return x * env(d, min(.3, d * .35), min(.35, d * .4)) * .55


def flute(n, d):
    f = midi(n) * (1 + .007 * np.sin(2 * np.pi * 5.2 * t_(d)) * np.clip(t_(d) * 4, 0, 1))
    return (tri(f, d) * .8 + .2 * sine(f * 2, d) + .05 * fft_filter(noise(d), 1500, 5000)) * env(d, .035, .08) * .8


def lead(n, d):
    return (pulse(midi(n), d, .25) * .5 + tri(midi(n), d) * .5) * env(d, .008, .05, .6) * .55


def brass(n, d):
    x = saw(midi(n), d) + saw(midi(n) * 1.005, d)
    return drive(fft_filter(x, 60, 1500), 1.6) * env(d, .03, .09) * .5


def bass(n, d):
    return (tri(midi(n), d) + .3 * pulse(midi(n), d)) * env(d, .006, .05, .5) * .7


def drum(kind):
    if kind == "taiko":
        return mix(drive(thump(.6, 130, 48, .2), 2.5), (fft_filter(noise(.1), 100, 900) * env(.1, .001, .03, .03), 0, .7))
    if kind == "taiko2":
        return mix(drive(thump(.4, 190, 85, .11), 2), (fft_filter(noise(.07), 200, 1500) * env(.07, .001, .02, .02), 0, .6))
    if kind == "war":
        return reverb(drive(thump(1.0, 85, 34, .38), 2), .9, .3, tail=False)
    if kind == "snare":
        return mix(fft_filter(noise(.18), 900, 6500) * env(.18, .001, .04, .055), (thump(.1, 220, 140, .04), 0, .6))
    if kind == "hat":
        return fft_filter(noise(.06), 5000, 13000) * env(.06, .0005, .02, .016)
    if kind == "hand":
        return mix(thump(.16, 330, 190, .05), (fft_filter(noise(.05), 500, 2500) * env(.05, .001, .02, .01), 0, .5))
    if kind == "anvil":
        return clang(.35, 1480, .09) * .7
    return noise(.05)


class Song:
    """Secuenciador: tiempos en pulsos; lo que se pasa del final vuelve al inicio (bucle perfecto)."""

    def __init__(self, bpm, beats):
        self.b = 60 / bpm
        self.n = secs(beats * self.b)
        self.buf = np.zeros(self.n)
        self.cache = {}

    def put(self, x, beat, vol):
        i = secs(beat * self.b) % self.n
        idx = (i + np.arange(len(x))) % self.n
        np.add.at(self.buf, idx, x * vol)

    def note(self, inst, n, beat, beats, vol):
        key = (inst.__name__, n, round(beats, 4))
        if key not in self.cache:
            self.cache[key] = inst(n, max(.04, beats * self.b))
        self.put(self.cache[key], beat, vol)

    def hit(self, kind, beat, vol):
        if kind not in self.cache:
            self.cache[kind] = drum(kind)
        self.put(self.cache[kind], beat, vol)

    def pattern(self, kind, text, start, step, vol):
        for i, ch in enumerate(text):
            if ch != ".":
                self.hit(kind, start + i * step, vol * (1 if ch == "X" else .6))

    def out(self, wet=.25, size=1.6, peak=.8, rms=.13):
        x = reverb(self.buf, size, wet, loop=True)
        x = x - x.mean()
        x = np.tanh(x / np.sqrt((x ** 2).mean()) * rms * 1.15)     # volumen parejo entre canciones
        m = np.abs(x).max()
        return x * min(1, peak / m)


MIN, MAJ, DIM = (0, 3, 7), (0, 4, 7), (0, 3, 6)


def chords(text):
    """'Dm Bb F' -> [(raíz midi, acorde)]"""
    base = {"C": 48, "Db": 49, "D": 50, "Eb": 51, "E": 52, "F": 53, "Gb": 54, "G": 55, "Ab": 44, "A": 45, "Bb": 46, "B": 47}
    res = []
    for c in text.split():
        q = DIM if c.endswith("dim") else MIN if c.endswith("m") else MAJ
        res.append((base[c.replace("dim", "").replace("m", "")], q))
    return res


def melody(song, inst, text, start, vol, octave=0):
    """'D5:1 F5:.5 -:1' (nota:duración en pulsos, - = silencio)."""
    names = {"C": 0, "D": 2, "E": 4, "F": 5, "G": 7, "A": 9, "B": 11}
    pos = start
    for tok in text.split():
        n, d = tok.split(":")
        d = float(d)
        if n != "-":
            acc = n.count("#") - n.count("b")
            m = names[n[0]] + acc + 12 * (int(n[-1]) + 1) + 12 * octave
            song.note(inst, m, pos, d, vol)
        pos += d
    return pos


def musica_menu():
    s = Song(80, 64)
    prog = chords("Dm Bb F C Dm Gm A A Dm Bb F C Gm Dm A Dm")
    for i, (r, q) in enumerate(prog):
        b = i * 4
        notes = [r + x for x in q]
        for k, p in enumerate([0, 2, 1, 2, 0 + 3, 2, 1, 2]):                     # arpegio de laúd
            n = notes[p % 3] + (12 if p >= 3 else 0) + 12
            s.note(pluck, n, b + k * .5, 1.6, .34 if k % 4 else .42)
        s.note(strings, r - 12, b, 4.2, .42)
        s.note(strings, notes[1], b, 4.2, .20)
        if i >= 4:
            s.note(strings, notes[2], b, 4.2, .18)
        if i >= 8:
            for x in notes:
                s.note(choir, x + 12, b, 4.2, .16)
        if i % 2 == 0:
            s.hit("war", b, .55)
            s.hit("war", b + 3.5, .28)
    mel = "A4:2 G4:1 F4:1 F4:2 D4:2 F4:1.5 G4:.5 A4:2 G4:3 -:1 A4:2 D5:1 C5:1 Bb4:2 G4:2 A4:2 C#5:1 E5:1 C#5:3 -:1"
    melody(s, flute, mel, 0 + 16 - 16, .20)
    mel2 = "D5:2 C5:1 A4:1 Bb4:2 F4:2 A4:1 C5:1 F5:2 E5:3 -:1 D5:1.5 C5:.5 Bb4:2 A4:2 F4:2 E4:1 F4:1 G4:1 A4:1 D4:3 -:1"
    melody(s, flute, mel2, 32, .24)
    return s.out(.35, 2.2, rms=.11)


def musica_inicio():
    s = Song(105, 96)
    prog = chords("Am F C G Am F Em Em Dm Am F G Am F C G Am Dm E E F G Am Am")
    for i, (r, q) in enumerate(prog):
        b = i * 4
        notes = [r + x for x in q]
        for k, p in enumerate([0, 1, 2, 1, 0, 2, 1, 2]):
            s.note(pluck, notes[p] + 12, b + k * .5, 1.0, .30)
        s.note(bass, r - 12, b, 1.4, .42)
        s.note(bass, r - 12, b + 2, .9, .34)
        s.note(bass, r - 5, b + 3, .8, .28)
        s.note(strings, notes[0], b, 4.1, .14)
        s.note(strings, notes[2], b, 4.1, .12)
        s.pattern("hand", "X..xX.x." if i % 4 != 3 else "X..xX.xx", b, .5, .34)
        if i >= 8:
            s.pattern("hat", ".x.x.x.x", b, .5, .12)
        if i % 4 == 0:
            s.hit("taiko", b, .5)
    a = "E5:1.5 D5:.5 C5:1 A4:1 C5:1.5 A4:.5 F4:2 G4:1 C5:1 E5:1 D5:1 D5:2 B4:1 -:1 A4:1 C5:1 E5:1 A5:1 G5:1.5 F5:.5 E5:2 B4:1 E5:1 G5:1 F#5:1 E5:3 -:1"
    b_ = "F5:1.5 E5:.5 D5:1 A4:1 E5:1.5 D5:.5 C5:2 A4:1 C5:1 F5:1 A5:1 G5:2 D5:1 -:1"
    c = "C5:1 D5:1 E5:1 C5:1 F5:1.5 E5:.5 D5:1 C5:1 E5:2 G5:2 D5:3 -:1 A5:1.5 G5:.5 E5:1 C5:1 D5:1 F5:1 A5:2 G#5:2 E5:1 D5:1 E5:3 -:1 A5:1 F5:1 C5:1 F5:1 B5:1 G5:1 D5:1 G5:1 A5:2 E5:1 C5:1 A4:3 -:1"
    e = melody(s, flute, a, 0, .26)
    e = melody(s, flute, b_, e, .26)
    melody(s, flute, c, e, .24)
    return s.out(.25, 1.4, rms=.12)


def musica_medio():
    s = Song(120, 96)
    prog = chords("Em Em C D Em Em C B Em Em Am B C D Em Em Am Am Em Em C D B B")
    for i, (r, q) in enumerate(prog):
        b = i * 4
        notes = [r + x for x in q]
        for k, p in enumerate([0, 0, 1, 0, 2, 0, 1, 0]):                          # ostinato de cuerdas
            s.note(stac, notes[p] + 12, b + k * .5, .42, .34)
        s.note(bass, r - 12, b, .9, .5)
        s.note(bass, r - 12, b + 1.5, .5, .4)
        s.note(bass, r - 12, b + 2.5, .9, .42)
        s.pattern("taiko", "X..X..X.", b, .5, .62)
        s.pattern("taiko2", ".xx.xx.X" if i % 2 else ".xx.x.xx", b, .5, .36)
        if i % 4 == 3:
            s.pattern("taiko2", "xxxxXXXX", b + 2, .25, .4)
        if i >= 8:
            s.pattern("snare", "..X...X.", b, .5, .18)
        if i >= 4:
            s.note(brass, r, b, 1.9, .20)
            s.note(brass, notes[2], b, 1.9, .15)
            s.note(brass, notes[1] if i % 2 else notes[0], b + 2, 1.6, .18)
    m = ("E4:1.5 F#4:.5 G4:1 B4:1 A4:1 G4:1 F#4:2 G4:1.5 A4:.5 C5:2 B4:1 A4:1 F#4:2 "
         "E4:1.5 G4:.5 B4:2 E5:1.5 D5:.5 B4:2 C5:1 B4:1 A4:1 G4:1 F#4:1 G4:1 D#4:2")
    melody(s, lead, m, 32, .24)
    m2 = ("E5:1 C5:1 A4:2 C5:1 E5:1 A5:2 G5:1.5 F#5:.5 E5:2 B4:4 C5:1 D5:1 E5:1 G5:1 F#5:1 E5:1 D5:2 D#5:2 F#5:2 B4:3 -:1")
    melody(s, lead, m2, 64, .24)
    return s.out(.22, 1.2, rms=.14)


def musica_final():
    s = Song(135, 128)
    prog = chords("Cm Cm Db Cm Cm Cm Ab G Cm Ab Fm G Cm Db Gdim G " * 2)
    for i, (r, q) in enumerate(prog):
        b = i * 4
        notes = [r + x for x in q]
        for k, p in enumerate([0, 0, 0, 1, 0, 0, 2, 1]):
            s.note(stac, notes[p] + (12 if i % 8 < 4 else 24), b + k * .5, .4, .30)
        s.pattern("taiko", "X.xX.xX.", b, .5, .66)
        s.pattern("taiko2", "..x..x.x" if i % 2 else ".x.x..xx", b, .5, .38)
        if i % 4 == 3:
            s.pattern("taiko2", "XxXxXxXx", b + 2, .25, .42)
        for k in range(8):
            s.note(bass, r - 12, b + k * .5, .4, .36 if k % 2 == 0 else .24)
        for x in notes:
            s.note(choir, x + 12, b, 3.8, .17 if i >= 8 else .10)
        if i >= 16:
            s.note(brass, r, b, 1.2, .24)
            s.note(brass, notes[1], b + 1.5, .9, .2)
            s.note(brass, notes[2], b + 2.5, 1.2, .2)
        if i % 8 == 7:                                                            # disonancia
            s.note(strings, r + 13, b, 4, .18)
            s.note(strings, r + 18, b, 4, .16)
    m = ("C5:1 Eb5:1 D5:1 C5:1 G4:3 -:1 Db5:1 F5:1 Eb5:1 Db5:1 C5:3 -:1 Eb5:1.5 D5:.5 C5:1 G5:1 F5:1 Eb5:1 D5:2 C5:1 Eb5:1 Ab5:2 G5:1 F5:1 D5:2 "
         "C5:2 G5:2 Ab5:1 G5:1 Eb5:2 F5:1 Ab5:1 C6:2 B5:2 G5:2 C5:1 D5:1 Eb5:1 G5:1 F5:1 Db5:1 Ab4:2 G4:1 Bb4:1 Db5:1 F5:1 D5:1 F5:1 G5:2")
    melody(s, lead, m, 64, .22)
    return s.out(.25, 1.4, rms=.145)


def musica_jefe():
    s = Song(145, 128)
    prog = chords("Dm Eb Dm Dm Dm Eb Gm A Bb Gm Dm A Dm Eb A A Dm Bb Gm A Dm Bb Eb A Gm Dm Eb Bb Gm A Dm Dm")
    for i, (r, q) in enumerate(prog):
        b = i * 4
        notes = [r + x for x in q]
        for k, p in enumerate([0, 2, 1, 2, 0, 2, 1, 2] if i % 2 else [0, 1, 2, 1, 0, 1, 2, 1]):
            s.note(stac, notes[p] + 24, b + k * .5, .4, .26)
        for k in range(8):
            s.note(bass, r - 12 + (12 if k in (3, 6) else 0), b + k * .5, .42, .40 if k % 2 == 0 else .28)
        s.pattern("taiko", "X.XxX.Xx", b, .5, .68)
        s.pattern("snare", "..X...X.", b, .5, .30)
        s.pattern("hat", "xxxxxxxx", b, .5, .10)
        if i % 4 == 3:
            s.pattern("taiko2", "XXxXXxXX", b + 2, .25, .46)
        for x in notes:
            s.note(choir, x + 12, b, 3.9, .18)
        s.note(brass, r, b, .9, .30)
        s.note(brass, r, b + 1.5, .5, .24)
        s.note(brass, notes[2], b + 2, 1.6, .26)
        if i % 8 == 0:
            s.hit("war", b, .6)
    m1 = ("D5:1.5 E5:.5 F5:1 A5:1 G5:1 F5:1 Eb5:2 D5:1 F5:1 A5:1 D6:1 C6:1 A5:1 F5:2 D5:1.5 F5:.5 A5:2 Bb5:1 G5:1 Eb5:2 G5:1 Bb5:1 D6:1 Bb5:1 A5:2 C#5:2 "
          "D6:2 Bb5:1 F5:1 G5:1 Bb5:1 D6:2 A5:2 F5:1 D5:1 E5:1 C#5:1 A4:2 F5:1 E5:1 D5:1 A5:1 Bb5:1 G5:1 Eb5:2 A5:1 G5:1 E5:1 C#5:1 A5:3 -:1")
    melody(s, lead, m1, 0, .24)
    m2 = ("A5:1 D6:1 A5:1 F5:1 Bb5:1 D6:1 Bb5:1 F5:1 G5:1 Bb5:1 G5:1 D5:1 E5:1 A5:1 C#6:2 D6:1.5 C6:.5 A5:1 F5:1 F5:1 Bb5:1 D6:2 Eb6:1 D6:1 Bb5:1 G5:1 A5:2 C#6:2 "
          "D6:1 Bb5:1 G5:1 Bb5:1 A5:1 F5:1 D5:2 G5:1 Bb5:1 Eb6:2 D6:1 Bb5:1 F5:2 G5:1 A5:1 Bb5:1 G5:1 A5:1 G5:1 E5:1 C#5:1 D5:4 A5:2 D6:2")
    melody(s, lead, m2, 64, .24)
    return s.out(.22, 1.3, rms=.15)


def musica_tienda():
    s = Song(95, 64)
    prog = chords("F C Dm Bb F C Bb C F Am Dm Bb Gm C F F")
    for i, (r, q) in enumerate(prog):
        b = i * 4
        notes = [r + x for x in q]
        for k, p in enumerate([0, 2, 1, 2, 0, 1, 2, 1]):
            s.note(pluck, notes[p] + 12, b + k * .5, 1.2, .36)
        s.note(bass, r - 12, b, 1.2, .34)
        s.note(bass, r - 5, b + 2, 1.0, .28)
        s.pattern("hand", "X.x.Xxx.", b, .5, .30)
        if i % 2 == 1:
            s.hit("anvil", b + 1, .10)
            s.hit("anvil", b + 3, .14)
        s.note(strings, notes[1], b, 4, .08)
    m = ("C5:1 A4:.5 C5:.5 F5:2 E5:1 C5:1 G4:2 A4:1 D5:1 F5:1 E5:1 D5:3 -:1 C5:1 F5:1 A5:1.5 G5:.5 E5:1 G5:1 C5:2 D5:1 F5:1 Bb5:1 A5:1 G5:3 -:1 "
         "A5:1 F5:1 C5:2 E5:1 C5:1 A4:2 D5:1 E5:1 F5:1 A5:1 G5:1 F5:1 D5:2 Bb4:1 D5:1 G5:1 F5:1 E5:1 D5:1 C5:2 F5:3 -:1 F5:1 C5:1 F4:2")
    melody(s, flute, m, 0, .22)
    s.put(crackle(s.n / SR - .1, 9), 0, .10)                                     # chimenea
    return s.out(.2, 1.0, rms=.10)


def jingle_victoria():
    s = Song(132, 16)
    for n, b, d in [(67, 0, .5), (67, .5, .5), (67, 1, .5), (72, 1.5, 1.5), (70, 3, .5), (72, 3.5, .5), (74, 4, 1), (76, 5, 4.5)]:
        s.note(brass, n, b, d, .4)
        s.note(lead, n + 12, b, d, .25)
    for b, ch in [(1.5, (48, 55, 64)), (3, (46, 53, 62)), (4, (43, 50, 59)), (5, (48, 55, 60, 64))]:
        for n in ch:
            s.note(brass, n, b, 1.4 if b < 5 else 4.5, .18)
            if b >= 5:
                s.note(choir, n + 12, b, 4.5, .2)
    for b in (0, .5, 1, 1.5, 3, 4, 4.5, 4.75, 5):
        s.hit("taiko", b, .6)
    for k, n in enumerate((72, 76, 79, 84, 88, 91, 96)):
        s.note(pluck, n, 5 + k * .25, 2, .25)
    return finish(reverb(s.buf[:secs(11.5 * s.b)], 1.4, .3, tail=False), .85, .9)


def jingle_derrota():
    s = Song(70, 10)
    for n, b, d in [(65, 0, 1), (64, 1, 1), (62, 2, 1), (57, 3, 3)]:
        s.note(strings, n, b, d + .3, .6)
        s.note(strings, n - 12, b, d + .3, .6)
    s.note(choir, 62, 3, 3, .35)
    s.note(choir, 65, 3, 3, .3)
    for b in (0, 3):
        s.put(clang(2.2, 294, .9), b, .35)
        s.hit("war", b, .7)
    return finish(reverb(s.buf[:secs(6.6 * s.b)], 1.8, .35, tail=False), .8, 1.0)


def musica():
    for name, fn in [("musica_menu", musica_menu), ("musica_inicio", musica_inicio), ("musica_medio", musica_medio),
                     ("musica_final", musica_final), ("musica_jefe", musica_jefe), ("musica_tienda", musica_tienda),
                     ("jingle_victoria", jingle_victoria), ("jingle_derrota", jingle_derrota)]:
        x = fn()
        save("Musica", name, x)
        print(f"{name}: {len(x) / SR:.1f} s")


if __name__ == "__main__":
    what = sys.argv[1] if len(sys.argv) > 1 else "todo"
    if what in ("todo", "efectos"):
        efectos()
    if what in ("todo", "musica"):
        musica()
