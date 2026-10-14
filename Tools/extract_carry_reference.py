from pathlib import Path
import cv2


def main():
    source = Path(r'C:\Users\TAM\Downloads\FDown.vn_Tai_video_Facebook_720p (HD)_7650(1).mp4')
    output = Path(__file__).resolve().parents[1] / 'work/items-vehicles-20261008/video-reference'
    output.mkdir(parents=True, exist_ok=True)
    capture = cv2.VideoCapture(str(source), cv2.CAP_FFMPEG)
    if not capture.isOpened():
        raise RuntimeError(f'Cannot inspect source video: {source}')
    duration = capture.get(cv2.CAP_PROP_FRAME_COUNT) / capture.get(cv2.CAP_PROP_FPS)
    for fraction in (.1, .3, .5, .7, .9):
        seconds = duration * fraction
        capture.set(cv2.CAP_PROP_POS_MSEC, seconds * 1000)
        okay, frame = capture.read()
        if okay:
            target = output / f'carry-source-{seconds:.2f}s.png'
            cv2.imwrite(str(target), frame)
            print(target)
    capture.release()
    print(f'Duration: {duration:.2f}s')


if __name__ == '__main__':
    main()
