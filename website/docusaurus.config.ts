import {themes as prismThemes} from 'prism-react-renderer';
import type {Config} from '@docusaurus/types';
import type * as Preset from '@docusaurus/preset-classic';

const config: Config = {
  title: 'Ratatosk',
  tagline: 'A unified messaging framework for .NET',
  favicon: 'img/favicon.ico',

  future: {
    v4: true,
  },

  url: 'https://ratatosk.deveel.org',
  baseUrl: '/',

  organizationName: 'deveel',
  projectName: 'ratatosk',

  onBrokenLinks: 'warn',

  headTags: [
    {
      tagName: 'meta',
      attributes: {
        name: 'algolia-site-verification',
        content: '7E0BBB65DCBBD694',
      },
    },
    {
      tagName: 'link',
      attributes: {
        rel: 'apple-touch-icon',
        sizes: '180x180',
        href: '/img/apple-touch-icon.png',
      },
    },
    {
      tagName: 'link',
      attributes: {
        rel: 'icon',
        type: 'image/png',
        sizes: '32x32',
        href: '/img/icon-32.png',
      },
    },
    {
      tagName: 'link',
      attributes: {
        rel: 'icon',
        type: 'image/png',
        sizes: '192x192',
        href: '/img/icon-192.png',
      },
    },
    {
      tagName: 'link',
      attributes: {
        rel: 'icon',
        type: 'image/png',
        sizes: '512x512',
        href: '/img/icon-512.png',
      },
    },
  ],

  i18n: {
    defaultLocale: 'en',
    locales: ['en'],
  },

  presets: [
    [
      'classic',
      {
        docs: {
          path: '../docs',
          sidebarPath: './sidebars.ts',
          editUrl: 'https://github.com/deveel/ratatosk/edit/main/docs/',
          lastVersion: '1.1.4',
          versions: {
            current: {
              label: 'Next (unreleased)',
              path: 'next',
              banner: 'unreleased',
            },
            '1.1.4': {
              label: 'v1.1.4 (latest)',
              path: '',
              banner: 'none',
            },
            '1.1.3': {
              label: 'v1.1.3',
              banner: 'unmaintained',
            },
            '1.1.2': {
              label: 'v1.1.2',
              banner: 'unmaintained',
            },
            '1.1.1': {
              label: 'v1.1.1',
              banner: 'unmaintained',
            },
            '1.0.6': {
              label: 'v1.0.6',
              banner: 'unmaintained',
            },
            '1.0.5': {
              label: 'v1.0.5',
              banner: 'unmaintained',
            },
          },
        },
        blog: false,
        theme: {
          customCss: './src/css/custom.css',
        },
      } satisfies Preset.Options,
    ],
  ],

  themeConfig: {
    image: '/img/ratatosk-full-logo.png',
    algolia: {
      appId: 'JYSR40O1I0',
      apiKey: '479261bc4e1a490dec72ff2d553c229b',
      indexName: 'ratatosk',
      contextualSearch: false,
    },
    colorMode: {
      defaultMode: 'light',
      disableSwitch: false,
      respectPrefersColorScheme: true,
    },
    navbar: {
      logo: {
        alt: 'Ratatosk Logo',
        src: 'img/ratatosk-full-logo.png',
        width: 106,
        height: 45,
      },
      items: [
        {
          type: 'docsVersionDropdown',
          position: 'left',
        },
        {
          type: 'html',
          position: 'right',
          value: '<span class="header-right-group"><a class="header-github-link" href="https://github.com/deveel/ratatosk" aria-label="GitHub repository" target="_blank" rel="noopener noreferrer"></a><a class="header-deveel-link" href="https://deveel.org" aria-label="Deveel website" target="_blank" rel="noopener noreferrer"><img src="/img/deveel-logo.svg" alt="Deveel" class="header-deveel-logo" /></a></span>',
        },
      ],
    },
    footer: {
      style: 'dark',
      links: [
        {
          title: 'Documentation',
          items: [
            {
              label: 'Getting Started',
              to: '/docs/',
            },
            {
              label: 'Framework Overview',
              to: '/docs/framework-overview',
            },
            {
              label: 'Roadmap',
              to: '/docs/roadmap',
            },
          ],
        },
        {
          title: 'Packages',
          items: [
            {
              label: 'NuGet',
              href: 'https://www.nuget.org/profiles/ratatosk',
            },
            {
              label: 'GitHub Packages',
              href: 'https://github.com/deveel/ratatosk/packages',
            },
          ],
        },
        {
          title: 'Community',
          items: [
            {
              label: 'GitHub',
              href: 'https://github.com/deveel/ratatosk',
            },
            {
              label: 'Issues',
              href: 'https://github.com/deveel/ratatosk/issues',
            },
            {
              label: 'Deveel',
              href: 'https://deveel.org',
            },
          ],
        },
      ],
      copyright: `Copyright © ${new Date().getFullYear()} Antonello Provenzano.`,
    },
    prism: {
      theme: prismThemes.github,
      darkTheme: prismThemes.dracula,
      additionalLanguages: ['csharp'],
    },
  } satisfies Preset.ThemeConfig,

  plugins: [
    [
      '@docusaurus/plugin-client-redirects',
      {
        redirects: [],
      },
    ],
  ],
};

export default config;
