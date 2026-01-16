using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Zaczy.SongBook.MAUI;

public class SongPreviewJavascript
{
    public static string JavascriptTxt()
    {
        var autoScrollJs = @"
<script>
(function(){
  var rafId = null;
  var pos = 0;
  var speed = 50;
  var last = 0;

  // keep pos updated on manual scroll
  window.addEventListener('scroll', function(){
    pos = window.scrollY || window.pageYOffset || 0;
  }, { passive: true });

  // show controls when user touches near top (mobile)
  function maybeShowControls(evt){
    var y = (evt.touches && evt.touches[0] && evt.touches[0].clientY) || (evt.clientY || 0);
    if(y <= 120){ // threshold in px
      // navigate to custom scheme to notify native
      window.location.href = 'app://showControls';
    }
  }

  // base font size helpers
  window.getBaseFontSize = function(){
    var fs = window.getComputedStyle(document.documentElement).fontSize;
    return parseFloat(fs) || 17;
  };
  window.setBaseFontSize = function(px){
    px = Number(px) || 17;
    document.documentElement.style.fontSize = px + 'px';
    return px;
  };
  window.changeBaseFontSize = function(delta){
    var cur = window.getBaseFontSize();
    var next = Math.max(8, Math.min(72, cur + Number(delta)));
    window.setBaseFontSize(next);
    return next;
  };

  window.getScrollPosition = function(){
    return window.scrollY || window.pageYOffset || 0;
  };

  window.setScrollPosition = function(y){
    pos = Number(y) || 0;
    window.scrollTo(0, pos);
  };

  // startAutoScroll(pxPerSec, optionalStartY)
  window.startAutoScroll = function(pxPerSec, startY){
    speed = Number(pxPerSec) || 50;
    if (typeof startY === 'number') pos = startY;
    else pos = window.scrollY || window.pageYOffset || pos || 0;

    if(rafId) return;
    last = performance.now();

    // ensure initial position applied
    window.scrollTo(0, pos);

    function step(now){
      var dt = (now - last)/1000;
      last = now;
      pos += speed * dt;
      window.scrollTo(0, pos);
      if(window.innerHeight + pos >= document.body.scrollHeight - 1){
        window.stopAutoScroll();
        return;
      }
      rafId = requestAnimationFrame(step);
    }
    rafId = requestAnimationFrame(step);
  };

  window.stopAutoScroll = function(){
    if(rafId){ cancelAnimationFrame(rafId); rafId = null; }
  };

})();
</script>
";
        return autoScrollJs;
    }
}
