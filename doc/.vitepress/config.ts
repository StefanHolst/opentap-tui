import { defineConfig } from 'vitepress'

// https://vitepress.dev/reference/site-config
export default defineConfig({
  title: "OpenTAP TUI Documentation",
  description: "OpenTAP TUI Documentation",
  base: '/opentap-tui/',
  outDir: '../public',
  themeConfig: {
    editLink:{
      pattern: "https://github.com/StefanHolst/opentap-tui/doc/:path",
      text: 'Help improve this page!'
    },
    nav: [
      { text: 'OpenTAP', link: 'https://github.com/opentap/opentap' },
      { text: 'OpenTAP Homepage', link: 'https://www.opentap.io' }
    ],

    sidebar: [
      {
        text: "OpenTAP TUI",
        link: "/"
      },
      {
        text: 'Getting Started',
        items: [
          { text: 'Installing', link: '/Installing' },
          { text: 'Debugging', link: '/Debugging' }
        ]
      },
      {
        text: "Navigation",
        link: "/Navigation"
      },
      {
        text: "Known Issues",
        link: "/KnownIssues"
      }
    ]
  }
})
