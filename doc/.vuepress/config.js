module.exports = {
    title: 'OpenTAP TUI',
    description: 'Textual User Interface for OpenTAP',
    themeConfig: {
        repo: 'https://gitlab.com/OpenTAP/Plugins/keysight/opentap-tui',
        editLinks: true,
        editLinkText: 'Help improve this page!',
        docsDir: 'doc',
        nav: [
            { text: 'OpenTAP', link: 'https://gitlab.com/opentap/opentap' },
            { text: 'OpenTAP Homepage', link: 'https://www.opentap.io' }
        ],
        sidebar: [
            ['/', 'OpenTAP TUI'],
            {
                title: 'Getting Started',
                children: [
                    '/Installing.md',
                    '/Debugging.md',
                ]
            },
            '/Navigation.md',
            '/KnownIssues.md'
        ]
    },
    dest: '../public',
    base: '/Plugins/keysight/opentap-tui/'
}
