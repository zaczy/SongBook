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
  var scrollDelay = 0;

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
  window.startAutoScroll = function(pxPerSec, startY, scrollDelay){

    delay = Number(scrollDelay) || 0;
    if(delay > 0 && startY ==0) {
      setTimeout(function(){
        window.startAutoScroll(pxPerSec, startY);
      }, delay * 1000);
      return;
    }

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

  var addToLog = function(msg){
    //console.log('SongPreviewJavascript: ' + msg);
    //var logEl = document.getElementById('debugLog');
    //if(logEl) logEl.innerHTML += '<div>' + msg + '</div>';
  };

</script>
";

        // append scroll-direction detector JS to the auto-scroll JS so it's injected into the page
        var detectScrollJs = @"
<script>
(function(){
  var lastY = window.pageYOffset || document.documentElement.scrollTop || 0;
  var threshold = 10;
  var debounceMs = 120;
  var debounceTimer = null;
  window.addEventListener('scroll', function(){
    var y = window.pageYOffset || document.documentElement.scrollTop || 0;
    var dy = y - lastY;
    lastY = y;
    if (debounceTimer) return;
    debounceTimer = setTimeout(function(){ debounceTimer = null; }, debounceMs);
    if (dy < -threshold) {
      // user scrolled up
      addToLog('app://showBack');

      try { window.location.href = 'app://showBack'; } catch(e){}
    } else if (dy > threshold) {
      // user scrolled down
      addToLog('app://hideBack');
      try { window.location.href = 'app://hideBack'; } catch(e){}
    }
  }, { passive: true });

  var pinchActive = false;
  var startDist = 0;
  var lastScale = 1;

  function dist(t0, t1){
    var dx = t0.pageX - t1.pageX;
    var dy = t0.pageY - t1.pageY;
    return Math.sqrt(dx*dx + dy*dy);
  }

  window.addEventListener('touchstart', function(e){
      addToLog('touchstart ' + (e.touches ? e.touches.length : 'brak e.touches'));
    if(e.touches && e.touches.length === 2){
      pinchActive = true;
      startDist = dist(e.touches[0], e.touches[1]) || 1;
      lastScale = 1;
    }
  }, { passive: true });

  window.addEventListener('touchmove', function(e){
      addToLog('touchmove ' + (e.touches ? e.touches.length : 'brak e.touches'));
    if(pinchActive && e.touches && e.touches.length === 2){
      var d = dist(e.touches[0], e.touches[1]);
      lastScale = d / startDist;
    }
  }, { passive: true });

  window.addEventListener('touchend', function(e){
    if(!pinchActive) return;
    addToLog('touchend');
    pinchActive = false;
    // thresholds chosen to avoid accidental triggers
    if(lastScale >= 1.15){
      try { window.location.href = 'app://pinchIn'; } catch(e){}
    } else if(lastScale <= 0.85){
      try { window.location.href = 'app://pinchOut'; } catch(e){}
    }
    lastScale = 1;
  }, { passive: true });
})();

(function(){
  // getTopLine(): returns JSON string { index: number, text: string, lineHeight?: number, top?: number }
  window.getTopLine = function() {
    try {
      var paddingTop = 0; // jeśli masz stały nagłówek w body, ustaw tu offset w px
      var x = Math.max(1, Math.floor(window.innerWidth / 2));
      var y = 1 + paddingTop; // punkt tuż przy górnej krawędzi viewportu
      var scrollTop = window.pageYOffset || document.documentElement.scrollTop || 0;

      // Preferowane: linie jako elementy .lyrics-line
      var lines = document.querySelectorAll('.lyrics-line');
      if (lines && lines.length) {
        for (var i = 0; i < lines.length; i++) {
          var r = lines[i].getBoundingClientRect();
          // pierwszy element którego dół jest > 0 (czyli część widoczna) lub którego top >= 0
          if (r.bottom > 0) {
            return JSON.stringify({ index: i, text: (lines[i].innerText || lines[i].textContent || '').trim(), top: Math.round(r.top), scrollTop: Math.round(scrollTop), totalLines: lines.length  });
          }
        }
      }

      // Fallback: <pre> z tekstem wiersz-po-wierszu
      var pre = document.querySelector('pre');
      if (pre) {
        var style = window.getComputedStyle(pre);
        var lineHeight = parseFloat(style.lineHeight);
        if (isNaN(lineHeight) || lineHeight === 0) {
          var fs = parseFloat(style.fontSize) || 16;
          lineHeight = fs * 1.2;
        }
        var preRect = pre.getBoundingClientRect();
        var scrollTop = window.pageYOffset || document.documentElement.scrollTop || 0;
        var preTopInDoc = preRect.top + scrollTop;
        var relative = scrollTop - preTopInDoc;
        var index = Math.max(0, Math.floor(relative / lineHeight));
        var textLines = (pre.innerText || '').split(/\r?\n/);
        var text = textLines[index] || '';
        return JSON.stringify({ index: index, text: text.trim(), lineHeight: Math.round(lineHeight), scrollTop: Math.round(scrollTop), totalLines: lines.length });
      }

      // Ostateczny fallback — element pod punktem (x,y)
      var el = document.elementFromPoint(x, y);
      if (el) {
        var parent = el.closest('.lyrics-line') || el.closest('pre') || el;
        var all = Array.from(document.querySelectorAll('.lyrics-line'));
        var idx = all.indexOf(parent);
        if (idx !== -1) {
          return JSON.stringify({ index: idx, text: (parent.innerText || parent.textContent || '').trim(), scrollTop: Math.round(scrollTop), totalLines: lines.length });
        }
      }

      return JSON.stringify({ index: -1, text: '', scrollTop: Math.round(scrollTop) });
    } catch (e) {
      return JSON.stringify({ index: -1, text: '', error: (e && e.message) ? e.message : 'unknown' });
    }
  };
})();
</script>

<script>
(function(){
  window.getRemainingScrollInfo = function(){
    try {
      var scrollTop = window.pageYOffset || document.documentElement.scrollTop || 0;
      var viewport = window.innerHeight || document.documentElement.clientHeight || 0;
      var docHeight = Math.max(
        document.body.scrollHeight, document.documentElement.scrollHeight,
        document.body.offsetHeight, document.documentElement.offsetHeight,
        document.body.clientHeight, document.documentElement.clientHeight
      );

      var remainingPx = Math.max(0, docHeight - (scrollTop + viewport));
      // percent of the total scrollable distance remaining (0..100)
      var totalScrollable = Math.max(0, docHeight - viewport);
      var remainingPercent = totalScrollable > 0 ? Math.round(remainingPx / totalScrollable * 100) : 0;

      // remaining lines estimation for variable-font mode (.lyrics-line) or pre-mode
      var remainingLines = -1;

      var lines = document.querySelectorAll('.lyrics-line');
      if (lines && lines.length) {
        remainingLines = 0;
        for (var i = 0; i < lines.length; i++) {
          var r = lines[i].getBoundingClientRect();
          // count lines whose top is below the viewport bottom (not visible)
          if (r.top >= viewport - 1) {
            remainingLines++;
          }
        }
      } else {
        var pre = document.querySelector('pre');
        if (pre) {
          var textLines = (pre.innerText || '').split(/\r?\n/);
          var style = window.getComputedStyle(pre);
          var lineHeight = parseFloat(style.lineHeight);
          if (isNaN(lineHeight) || lineHeight === 0) {
            var fs = parseFloat(style.fontSize) || 16;
            lineHeight = fs * 1.2;
          }
          var preRect = pre.getBoundingClientRect();
          var preTopInDoc = preRect.top + (window.pageYOffset || document.documentElement.scrollTop || 0);
          var firstVisibleLine = Math.max(0, Math.floor(((window.pageYOffset || document.documentElement.scrollTop || 0) - preTopInDoc) / lineHeight));
          var visibleLines = Math.ceil(viewport / lineHeight);
          remainingLines = Math.max(0, textLines.length - (firstVisibleLine + visibleLines));
        }
      }

      return JSON.stringify({
        remainingPx: Math.round(remainingPx),
        remainingPercent: remainingPercent,
        remainingLines: remainingLines,
        docHeight: Math.round(docHeight),
        viewport: Math.round(viewport),
        scrollTop: Math.round(scrollTop)
      });
    } catch (e) {
      return JSON.stringify({ error: (e && e.message) ? e.message : 'unknown' });
    }
  };
})();
</script>
";


        return autoScrollJs + detectScrollJs;
    }
}
