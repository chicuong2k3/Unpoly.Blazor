// Unpoly inserts DOM that DOMContentLoaded will never see. Compilers run on every insertion,
// including the initial page load. Return a destructor for anything global, or each swap
// leaks another instance with nothing visible to tell you.
// 📖 https://unpoly.com/up.compiler
up.compiler('[data-clock]', (element) => {
  const tick = () => element.textContent = new Date().toLocaleTimeString()
  tick()
  const timer = setInterval(tick, 1000)
  return () => clearInterval(timer)
})
