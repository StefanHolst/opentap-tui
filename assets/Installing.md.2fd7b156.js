import{_ as e,c as a,o as i,a as o}from"./app.ced3e445.js";const f=JSON.parse('{"title":"Installing","description":"","frontmatter":{},"headers":[{"level":2,"title":"Install from Repository","slug":"install-from-repository","link":"#install-from-repository","children":[]},{"level":2,"title":"From Source","slug":"from-source","link":"#from-source","children":[]},{"level":2,"title":"Via Docker","slug":"via-docker","link":"#via-docker","children":[]}],"relativePath":"Installing.md"}'),t={name:"Installing.md"},l=o('<h1 id="installing" tabindex="-1">Installing <a class="header-anchor" href="#installing" aria-hidden="true">#</a></h1><p>Either build it from source or install it from the OpenTAP package repository (<a href="packages.opentap.io">packages.opentap.io</a>).</p><h2 id="install-from-repository" tabindex="-1">Install from Repository <a class="header-anchor" href="#install-from-repository" aria-hidden="true">#</a></h2><ul><li>Install: <code>tap package install TUI</code></li><li>Run: <code>tap tui</code></li><li>Run Package Manager: <code>tap tui-pm</code></li><li>Run Results Viewer: <code>tap tui-results</code></li></ul><h2 id="from-source" tabindex="-1">From Source <a class="header-anchor" href="#from-source" aria-hidden="true">#</a></h2><ul><li>Build: <code>dotnet build</code></li><li>Run: <code>OpenTAP.TUI/bin/Debug/tap tui</code></li><li>Run Package Manager: <code>OpenTAP.TUI/bin/Debug/tap tui-pm</code></li><li>Run Results Viewer: <code>OpenTAP.TUI/bin/Debug/tap tui-results</code></li></ul><h2 id="via-docker" tabindex="-1">Via Docker <a class="header-anchor" href="#via-docker" aria-hidden="true">#</a></h2><ul><li>See <a href="https://hub.docker.com/r/opentapio/opentap/tags" target="_blank" rel="noreferrer">https://hub.docker.com/r/opentapio/opentap/tags</a> for available OpenTap Docker images, the following example uses <code>9.15-ubuntu18.04</code>.</li><li>Run: <code>docker pull opentapio/opentap:9.18.3-bionic</code></li><li>Run: <code>docker run --name mytui -it opentapio/opentap:9.18.3-bionic</code></li><li>Run: <code>tap package install TUI</code></li><li>Run: <code>tap tui</code></li></ul>',8),r=[l];function n(c,s,d,p,u,h){return i(),a("div",null,r)}const g=e(t,[["render",n]]);export{f as __pageData,g as default};
