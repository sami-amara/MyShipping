(function($) {
  'use strict';
  //Open submenu on hover in compact sidebar mode and horizontal menu mode
  $(document).on('mouseenter mouseleave', '.sidebar .nav-item', function(ev) {
    const body = $('body');
    const sidebarIconOnly = body.hasClass('sidebar-icon-only');
    const sidebarFixed = body.hasClass('sidebar-fixed');
    if (!('ontouchstart' in document.documentElement)) {
      if (sidebarIconOnly) {
        if (sidebarFixed) {
          if (ev.type === 'mouseenter') {
            body.removeClass('sidebar-icon-only');
          }
        } else {
          const $menuItem = $(this);
          if (ev.type === 'mouseenter') {
            $menuItem.addClass('hover-open');
          } else {
            $menuItem.removeClass('hover-open');
          }
        }
      }
    }
  });
  $('.aside-toggler').click(function(){
    $('.chat-list-wrapper').toggleClass('slide');
  });
})(jQuery);