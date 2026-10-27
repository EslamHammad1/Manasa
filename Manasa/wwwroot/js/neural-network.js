(() => {
  "use strict";

  const canvas = document.querySelector("[data-neural-network]");
  if (!canvas || canvas.dataset.initialized === "true") return;
  canvas.dataset.initialized = "true";

  const context = canvas.getContext("2d", { alpha: true });
  if (!context) return;

  const reducedMotion = window.matchMedia("(prefers-reduced-motion: reduce)");
  const touchDevice = window.matchMedia("(hover: none)");
  const config = {
    desktopDensity: 1 / 12500,
    mobileDensity: 1 / 22000,
    maxParticles: 92,
    minParticles: 26,
    connectionDistance: 142,
    attractionRadius: 190,
    attractionStrength: 0.026,
    damping: 0.965,
    maxSpeed: 1.25
  };

  let width = 0;
  let height = 0;
  let pixelRatio = 1;
  let particles = [];
  let animationFrame = 0;
  let lastTime = 0;
  let pointer = { x: -1000, y: -1000, active: false };
  let isVisible = !document.hidden;

  const random = (min, max) => min + Math.random() * (max - min);
  const clamp = (value, min, max) => Math.max(min, Math.min(max, value));

  function resize() {
    const previousWidth = width;
    const previousHeight = height;
    const previousParticles = particles;
    width = window.innerWidth;
    height = window.innerHeight;
    pixelRatio = Math.min(window.devicePixelRatio || 1, 1.5);
    canvas.width = Math.round(width * pixelRatio);
    canvas.height = Math.round(height * pixelRatio);
    context.setTransform(pixelRatio, 0, 0, pixelRatio, 0, 0);

    const mobile = width < 700 || touchDevice.matches;
    const density = mobile ? config.mobileDensity : config.desktopDensity;
    const count = reducedMotion.matches
      ? Math.min(32, config.minParticles)
      : clamp(Math.round(width * height * density), config.minParticles, config.maxParticles);

    particles = Array.from({ length: count }, (_, index) => {
      const previous = previousParticles[index];
      if (previous && previousWidth && previousHeight) {
        return {
          ...previous,
          x: previous.x * width / previousWidth,
          y: previous.y * height / previousHeight
        };
      }
      return {
        x: random(0, width),
        y: random(0, height),
        vx: random(-0.35, 0.35),
        vy: random(-0.35, 0.35),
        phase: random(0, Math.PI * 2),
        size: random(1.1, 2.1),
        orbit: 24 + (index % 9) * 8,
        angle: random(0, Math.PI * 2)
      };
    });
    draw(0, true);
  }

  function draw(time, still = false) {
    context.clearRect(0, 0, width, height);
    const delta = lastTime ? Math.min((time - lastTime) / 16.67, 2) : 1;
    lastTime = time;

    if (!still && !reducedMotion.matches) {
      for (const particle of particles) {
        particle.phase += 0.012 * delta;
        particle.vx += Math.sin(particle.phase) * 0.002 * delta;
        particle.vy += Math.cos(particle.phase * 0.8) * 0.002 * delta;

        if (pointer.active) {
          const dx = pointer.x - particle.x;
          const dy = pointer.y - particle.y;
          const distance = Math.hypot(dx, dy);
          if (distance < config.attractionRadius && distance > 0) {
            const falloff = 1 - distance / config.attractionRadius;
            const targetX = pointer.x + Math.cos(particle.angle + time * 0.00018) * particle.orbit;
            const targetY = pointer.y + Math.sin(particle.angle + time * 0.00018) * particle.orbit;
            particle.vx += (targetX - particle.x) * config.attractionStrength * falloff * delta;
            particle.vy += (targetY - particle.y) * config.attractionStrength * falloff * delta;
          }
        }

        particle.vx *= config.damping;
        particle.vy *= config.damping;
        const speed = Math.hypot(particle.vx, particle.vy);
        if (speed > config.maxSpeed) {
          particle.vx = particle.vx / speed * config.maxSpeed;
          particle.vy = particle.vy / speed * config.maxSpeed;
        }
        particle.x += particle.vx * delta;
        particle.y += particle.vy * delta;

        if (particle.x < -8) particle.x = width + 8;
        else if (particle.x > width + 8) particle.x = -8;
        if (particle.y < -8) particle.y = height + 8;
        else if (particle.y > height + 8) particle.y = -8;
      }
    }

    const connectionDistance = config.connectionDistance;
    for (let i = 0; i < particles.length; i++) {
      const a = particles[i];
      for (let j = i + 1; j < particles.length; j++) {
        const b = particles[j];
        const dx = b.x - a.x;
        const dy = b.y - a.y;
        const distance = Math.hypot(dx, dy);
        if (distance >= connectionDistance) continue;
        const alpha = (1 - distance / connectionDistance) * 0.19;
        context.strokeStyle = `rgba(91, 203, 193, ${alpha})`;
        context.lineWidth = 0.7;
        context.beginPath();
        context.moveTo(a.x, a.y);
        context.lineTo(b.x, b.y);
        context.stroke();

        const signal = (time * 0.000035 + i * 0.17 + j * 0.07) % 1;
        if (signal < 0.11 && distance < connectionDistance * 0.78) {
          const pulse = signal / 0.11;
          context.fillStyle = `rgba(153, 244, 224, ${0.38 * (1 - pulse)})`;
          context.beginPath();
          context.arc(a.x + dx * pulse, a.y + dy * pulse, 1.15, 0, Math.PI * 2);
          context.fill();
        }
      }

      const glow = pointer.active && Math.hypot(pointer.x - a.x, pointer.y - a.y) < config.attractionRadius;
      context.fillStyle = glow ? "rgba(144, 239, 221, .88)" : "rgba(128, 216, 211, .62)";
      if (glow) {
        context.shadowColor = "rgba(93, 226, 207, .72)";
        context.shadowBlur = 10;
      }
      context.beginPath();
      context.arc(a.x, a.y, a.size, 0, Math.PI * 2);
      context.fill();
      context.shadowBlur = 0;
    }

    if (pointer.active && !reducedMotion.matches) {
      const gradient = context.createRadialGradient(pointer.x, pointer.y, 0, pointer.x, pointer.y, config.attractionRadius);
      gradient.addColorStop(0, "rgba(81, 202, 190, .035)");
      gradient.addColorStop(1, "rgba(81, 202, 190, 0)");
      context.fillStyle = gradient;
      context.fillRect(pointer.x - config.attractionRadius, pointer.y - config.attractionRadius, config.attractionRadius * 2, config.attractionRadius * 2);
    }
  }

  function animate(time) {
    if (!isVisible) return;
    draw(time);
    animationFrame = window.requestAnimationFrame(animate);
  }

  function start() {
    if (isVisible && !animationFrame) animationFrame = window.requestAnimationFrame(animate);
  }

  function stop() {
    if (animationFrame) window.cancelAnimationFrame(animationFrame);
    animationFrame = 0;
  }

  function updatePointer(event) {
    if (event.pointerType === "touch" && event.type !== "pointermove") return;
    pointer = { x: event.clientX, y: event.clientY, active: true };
  }

  function clearPointer() {
    pointer.active = false;
  }

  function pointerLeftViewport(event) {
    if (!event.relatedTarget) clearPointer();
  }

  function visibilityChanged() {
    isVisible = !document.hidden;
    if (isVisible) start();
    else stop();
  }

  function motionPreferenceChanged() {
    resize();
    if (reducedMotion.matches) {
      stop();
      draw(0, true);
    } else {
      lastTime = 0;
      start();
    }
  }

  function cleanup() {
    stop();
    window.removeEventListener("resize", resize);
    window.removeEventListener("pointermove", updatePointer);
    window.removeEventListener("pointerout", pointerLeftViewport);
    window.removeEventListener("pointerup", clearPointer);
    window.removeEventListener("pointercancel", clearPointer);
    window.removeEventListener("pagehide", cleanup);
    document.removeEventListener("visibilitychange", visibilityChanged);
    reducedMotion.removeEventListener?.("change", motionPreferenceChanged);
    touchDevice.removeEventListener?.("change", resize);
    removalObserver.disconnect();
  }

  const removalObserver = new MutationObserver(() => {
    if (!document.documentElement.contains(canvas)) cleanup();
  });
  removalObserver.observe(document.documentElement, { childList: true, subtree: true });

  window.addEventListener("resize", resize, { passive: true });
  window.addEventListener("pointermove", updatePointer, { passive: true });
  window.addEventListener("pointerout", pointerLeftViewport, { passive: true });
  window.addEventListener("pointerup", clearPointer, { passive: true });
  window.addEventListener("pointercancel", clearPointer, { passive: true });
  window.addEventListener("pagehide", cleanup, { once: true });
  document.addEventListener("visibilitychange", visibilityChanged);
  reducedMotion.addEventListener?.("change", motionPreferenceChanged);
  touchDevice.addEventListener?.("change", resize);

  resize();
  if (!reducedMotion.matches) start();
})();
