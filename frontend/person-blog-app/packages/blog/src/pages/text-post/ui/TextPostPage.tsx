import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import defaultProfilePic from '@/shared/assets/defaultProfilePic.png';
import { Button } from '@/shared/ui/Button/Button';
import { PageShell } from '@/widgets/page-shell';
import styles from './TextPostPage.module.css';

type Reaction = 'like' | 'dislike' | null;

const TextPostPage = () => {
  const navigate = useNavigate();
  const [reaction, setReaction] = useState<Reaction>(null);
  const [isSubscribed, setIsSubscribed] = useState(false);

  const likes = 128 + (reaction === 'like' ? 1 : 0);
  const dislikes = 4 + (reaction === 'dislike' ? 1 : 0);

  const handleBack = () => {
    if (window.history.length > 1) {
      navigate(-1);
      return;
    }

    navigate('/');
  };

  const toggleReaction = (nextReaction: Exclude<Reaction, null>) => {
    setReaction(currentReaction => currentReaction === nextReaction ? null : nextReaction);
  };

  return (
    <PageShell className={styles.shell} contentClassName={styles.page}>
      <div className={styles.layout}>
        <article className={styles.article}>
          <header className={styles.articleHeader}>
            <button className={styles.backButton} type="button" onClick={handleBack}>
              <ArrowLeftIcon />
              Назад
            </button>

            <span className={styles.category}>Разработка</span>
            <h1>Как создавать интерфейсы, которые остаются понятными</h1>
            <p className={styles.lead}>
              Практическое руководство о визуальной иерархии, типографике и деталях,
              из которых складывается цельный продукт.
            </p>
            <div className={styles.meta}>
              <time dateTime="2026-08-21">21 августа 2026</time>
              <span aria-hidden="true" />
              <span>8 минут чтения</span>
              <span aria-hidden="true" />
              <span>1 284 просмотра</span>
            </div>
          </header>

          <HeroIllustration />

          <div className={styles.content}>
            <p>
              Хороший интерфейс не требует от человека угадывать, что произойдёт дальше.
              Он <strong>показывает главное</strong>, объясняет второстепенное и спокойно
              скрывает детали до того момента, когда они действительно понадобятся.
            </p>
            <p>
              В этой статье собраны примеры основных возможностей форматирования:
              <em> курсив для интонации</em>, <strong>жирное начертание для акцентов</strong>,{' '}
              <u>подчёркивание</u>, <s>зачёркнутый текст</s>, <mark>мягкое выделение</mark>{' '}
              и <a href="#principles">ссылки внутри материала</a>.
            </p>

            <h2 id="principles">Сначала — ясная иерархия</h2>
            <p>
              Пользователь сканирует страницу быстрее, чем читает её. Поэтому заголовок,
              вводный абзац, изображение и основное действие должны складываться в понятный
              маршрут взгляда.
            </p>
            <ul>
              <li><strong>Один главный заголовок</strong> формулирует тему страницы.</li>
              <li>Подзаголовки делят длинный материал на самостоятельные смысловые блоки.</li>
              <li>Вторичный текст остаётся контрастным, но не спорит с основным.</li>
            </ul>

            <blockquote>
              Если все элементы выглядят одинаково важными, пользователю приходится
              самостоятельно восстанавливать структуру страницы.
            </blockquote>

            <h3>Ритм важнее количества украшений</h3>
            <p>
              Последовательные интервалы создают ощущение порядка. В проекте их удобно
              хранить в токенах: например, <code>--space-4</code> для базового отступа и{' '}
              <code>--radius-lg</code> для крупных поверхностей.
            </p>

            <figure>
              <ContentIllustration />
              <figcaption>
                Один сильный акцент, спокойный контекст и предсказуемая группа действий.
              </figcaption>
            </figure>

            <h2>Компоненты должны вести себя одинаково</h2>
            <p>
              Одинаковые действия заслуживают одинакового оформления. Если оранжевая
              кнопка означает основное действие, не стоит использовать её для удаления
              или нейтральной навигации.
            </p>
            <ol>
              <li>Определите роль компонента.</li>
              <li>Выберите устойчивое визуальное состояние.</li>
              <li>Добавьте состояния наведения, фокуса, загрузки и ошибки.</li>
              <li>Проверьте компонент на узком экране и с длинным текстом.</li>
            </ol>

            <div className={styles.tableWrapper}>
              <table>
                <thead>
                  <tr><th>Элемент</th><th>Назначение</th><th>Акцент</th></tr>
                </thead>
                <tbody>
                  <tr><td>Основная кнопка</td><td>Продолжить сценарий</td><td>Высокий</td></tr>
                  <tr><td>Вторичная кнопка</td><td>Дополнительное действие</td><td>Средний</td></tr>
                  <tr><td>Текстовая ссылка</td><td>Переход к контексту</td><td>Низкий</td></tr>
                </tbody>
              </table>
            </div>

            <h3>Небольшой пример</h3>
            <p>Даже короткий фрагмент стилей проще поддерживать, если он опирается на общие переменные:</p>
            <pre><code>{`.primary-action {
  background: var(--color-brand-500);
  color: #fff;
  border-radius: var(--radius-sm);
}`}</code></pre>

            <hr />
            <h2>Итог</h2>
            <p>
              Цельная страница получается не из одного эффектного решения, а из множества
              небольших согласованных выборов. Начните с иерархии, закрепите правила в
              компонентах и проверяйте, что контент остаётся удобным при любой длине и на
              любом экране.
            </p>
          </div>

          <footer className={styles.articleFooter}>
            <div className={styles.reactions} aria-label="Оценка статьи">
              <button
                className={`${styles.reactionButton} ${reaction === 'like' ? styles.reactionActive : ''}`}
                type="button"
                aria-pressed={reaction === 'like'}
                onClick={() => toggleReaction('like')}
              >
                <LikeIcon />
                <span>Нравится</span>
                <strong>{likes}</strong>
              </button>
              <button
                className={`${styles.reactionButton} ${styles.dislikeButton} ${reaction === 'dislike' ? styles.dislikeActive : ''}`}
                type="button"
                aria-pressed={reaction === 'dislike'}
                onClick={() => toggleReaction('dislike')}
              >
                <DislikeIcon />
                <span>Не нравится</span>
                <strong>{dislikes}</strong>
              </button>
            </div>
            <button className={styles.shareButton} type="button">
              <ShareIcon />
              Поделиться
            </button>
          </footer>
        </article>

        <aside className={styles.creator} aria-labelledby="creator-name">
          <p className={styles.creatorEyebrow}>Создатель публикации</p>
          <div className={styles.creatorMain}>
            <img src={defaultProfilePic} alt="" />
            <div>
              <span id="creator-name">PlayView Design</span>
              <p>12,4 тыс. подписчиков</p>
            </div>
          </div>
          <p className={styles.creatorDescription}>
            Пишем о дизайне цифровых продуктов, разработке и понятных интерфейсах.
          </p>
          <Button
            variant={isSubscribed ? 'secondary' : 'primary'}
            fullWidth
            aria-pressed={isSubscribed}
            onClick={() => setIsSubscribed(value => !value)}
          >
            {isSubscribed ? 'Вы подписаны' : 'Подписаться'}
          </Button>
          <div className={styles.creatorStats}>
            <div><strong>48</strong><span>публикаций</span></div>
            <div><strong>1,2 млн</strong><span>просмотров</span></div>
          </div>
        </aside>
      </div>
    </PageShell>
  );
};

const HeroIllustration = () => (
  <div className={styles.hero} role="img" aria-label="Карточки интерфейса на оранжевом фоне">
    <svg viewBox="0 0 1200 638" xmlns="http://www.w3.org/2000/svg" preserveAspectRatio="xMidYMid slice">
      <defs>
        <linearGradient id="article-hero-bg" x1="0" y1="0" x2="1" y2="1">
          <stop stopColor="#ff9a3d" /><stop offset="1" stopColor="#d95f00" />
        </linearGradient>
        <filter id="article-hero-shadow" x="-20%" y="-20%" width="140%" height="140%">
          <feDropShadow dx="0" dy="18" stdDeviation="18" floodColor="#7c2d12" floodOpacity=".26" />
        </filter>
      </defs>
      <rect width="1200" height="638" fill="url(#article-hero-bg)" />
      <circle cx="1030" cy="92" r="235" fill="#fff" opacity=".1" />
      <circle cx="120" cy="590" r="250" fill="#7c2d12" opacity=".12" />
      <g filter="url(#article-hero-shadow)" transform="translate(170 95) rotate(-4 270 205)">
        <rect width="540" height="410" rx="28" fill="#fff" />
        <rect x="34" y="34" width="180" height="18" rx="9" fill="#e4e4e7" />
        <rect x="34" y="78" width="472" height="180" rx="18" fill="#27272a" />
        <rect x="34" y="286" width="330" height="15" rx="7" fill="#d4d4d8" />
        <rect x="34" y="320" width="440" height="12" rx="6" fill="#e4e4e7" />
        <rect x="34" y="350" width="390" height="12" rx="6" fill="#e4e4e7" />
      </g>
      <g filter="url(#article-hero-shadow)" transform="translate(675 155) rotate(6 180 150)">
        <rect width="360" height="300" rx="24" fill="#fff7ed" />
        <circle cx="62" cy="64" r="28" fill="#ff7b00" />
        <rect x="108" y="47" width="165" height="14" rx="7" fill="#a1a1aa" />
        <rect x="108" y="75" width="112" height="11" rx="5" fill="#d4d4d8" />
        <rect x="34" y="128" width="292" height="12" rx="6" fill="#d4d4d8" />
        <rect x="34" y="158" width="250" height="12" rx="6" fill="#e4e4e7" />
        <rect x="34" y="188" width="275" height="12" rx="6" fill="#e4e4e7" />
        <rect x="34" y="234" width="120" height="36" rx="18" fill="#ff7b00" />
      </g>
    </svg>
  </div>
);

const ContentIllustration = () => (
  <div className={styles.contentImage} role="img" aria-label="Схема визуальной иерархии из трёх уровней">
    <svg viewBox="0 0 1000 563" xmlns="http://www.w3.org/2000/svg" preserveAspectRatio="xMidYMid slice">
      <defs>
        <linearGradient id="article-content-bg" x1="0" y1="0" x2="1" y2="1">
          <stop stopColor="#fff7ed" /><stop offset="1" stopColor="#fed7aa" />
        </linearGradient>
      </defs>
      <rect width="1000" height="563" fill="url(#article-content-bg)" />
      <g transform="translate(110 88)">
        <rect width="780" height="388" rx="28" fill="#fff" stroke="#dedee1" />
        <circle cx="66" cy="67" r="25" fill="#ff7b00" />
        <rect x="112" y="49" width="360" height="25" rx="12" fill="#18181b" />
        <rect x="112" y="90" width="265" height="13" rx="6" fill="#a1a1aa" />
        <rect x="50" y="155" width="680" height="92" rx="16" fill="#f5f5f6" />
        <rect x="76" y="179" width="305" height="17" rx="8" fill="#52525b" />
        <rect x="76" y="213" width="520" height="11" rx="5" fill="#d4d4d8" />
        <rect x="50" y="280" width="208" height="58" rx="12" fill="#ff7b00" />
        <rect x="282" y="280" width="208" height="58" rx="12" fill="#f5f5f6" />
        <rect x="514" y="280" width="216" height="58" rx="12" fill="#f5f5f6" />
      </g>
    </svg>
  </div>
);

const ArrowLeftIcon = () => (
  <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" aria-hidden="true">
    <path d="m15 18-6-6 6-6" />
  </svg>
);

const LikeIcon = () => (
  <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" aria-hidden="true">
    <path d="M7 10v11M3 10h4l3-7a2 2 0 0 1 2 2v5h6a2 2 0 0 1 2 2l-2 7a2 2 0 0 1-2 2H7" />
  </svg>
);

const DislikeIcon = () => (
  <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" aria-hidden="true">
    <path d="M17 14V3M21 14h-4l-3 7a2 2 0 0 1-2-2v-5H6a2 2 0 0 1-2-2l2-7a2 2 0 0 1 2-2h9" />
  </svg>
);

const ShareIcon = () => (
  <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" aria-hidden="true">
    <circle cx="18" cy="5" r="3" /><circle cx="6" cy="12" r="3" /><circle cx="18" cy="19" r="3" />
    <path d="m8.6 10.5 6.8-4M8.6 13.5l6.8 4" />
  </svg>
);

export default TextPostPage;
