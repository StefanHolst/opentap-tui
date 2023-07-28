module.exports = {
    title: 'OpenTAP Python Integration',
    description: 'Python Integration for OpenTAP',
    
    themeConfig: {
        repo: 'https://github.com/StefanHolst/opentap-tui/',
        editLinks: true,
        editLinkText: 'Help improve this page!',
        docsBranch: 'main',
        docsDir: 'Documentation',
        nav: [
            { text: 'OpenTAP', link: 'https://github.com/opentap/opentap' },
            { text: 'OpenTAP Homepage', link: 'https://www.opentap.io' }
        ],
        sidebar: [
            ['Index.md', "Welcome"],
            ['Debugging.md', 'Debugging'],
            ['Installing.md', 'Installing'],
            ['KnownIssues-md', 'Known issues'],
            ['Navigation.md', 'Navigation'],
        ]
    },
    dest: '../public',
    base: '/OpenTap.Tui/'
}

