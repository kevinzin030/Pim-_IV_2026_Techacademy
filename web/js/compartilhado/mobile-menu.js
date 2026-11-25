/**
 * Controla a navegação lateral em telas pequenas.
 * O HTML define os elementos e este arquivo apenas alterna classes e atributos.
 */
(() => {
      const sidebar = document.getElementById('sidebar');
      const toggle = document.getElementById('mobile-menu-toggle');
      const backdrop = document.getElementById('mobile-menu-backdrop');
      if (!sidebar || !toggle || !backdrop) return;
      const closeMenu = () => {
        sidebar.classList.remove('mobile-menu-open');
        backdrop.classList.remove('mobile-menu-backdrop-visible');
        toggle.setAttribute('aria-expanded', 'false');
        toggle.setAttribute('aria-label', 'Abrir menu de navegação');
        // Atualiza apenas a classe do ícone existente; não gera HTML via JavaScript.
        const icon = toggle.querySelector('i');
        if (icon) icon.className = 'fa-solid fa-bars';
        document.body.classList.remove('mobile-menu-lock');
      };
      const openMenu = () => {
        sidebar.classList.add('mobile-menu-open');
        backdrop.classList.add('mobile-menu-backdrop-visible');
        toggle.setAttribute('aria-expanded', 'true');
        toggle.setAttribute('aria-label', 'Fechar menu de navegação');
        // Reutiliza o mesmo elemento de ícone para indicar que o menu pode ser fechado.
        const icon = toggle.querySelector('i');
        if (icon) icon.className = 'fa-solid fa-xmark';
        document.body.classList.add('mobile-menu-lock');
      };
      toggle.addEventListener('click', () => sidebar.classList.contains('mobile-menu-open') ? closeMenu() : openMenu());
      backdrop.addEventListener('click', closeMenu);
      sidebar.querySelectorAll('nav button').forEach(button => button.addEventListener('click', () => { if (window.matchMedia('(max-width: 767px)').matches) closeMenu(); }));
      document.addEventListener('keydown', event => { if (event.key === 'Escape') closeMenu(); });
      window.addEventListener('resize', () => { if (window.innerWidth >= 768) closeMenu(); });
    })();
